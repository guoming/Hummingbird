using Confluent.Kafka;
using Hummingbird.Extersions.EventBus.Abstractions;
using Hummingbird.Extersions.EventBus.Models;
using Hummingbird.LoadBalancers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hummingbird.Extersions.EventBus.Kafka.Extersions;

namespace Hummingbird.Extersions.EventBus.Kafka
{

    /// <summary>
    /// 消息队列
    /// 作者：郭明
    /// 日期：2017年4月5日
    /// </summary>
    public class EventBusKafka : IEventBus
    {
        public struct EventMessage
        {
            public long EventId { get; set; }

            public string MessageId { get; set; }

            public string TraceId { get; set; }

            public string Body { get; set; }

            public string RouteKey { get; set; }

            public Timestamp Timestamp { get; set; }

            public IDictionary<string, object> Headers { get; set; }

        }
        private readonly IServiceProvider _lifetimeScope;
        private readonly ILogger<IEventBus> _logger;
        private readonly int _reveiverMaxDegreeOfParallelism;
        private readonly string _compomentName = typeof(EventBusKafka).FullName;

        private readonly ILoadBalancer<IKafkaPersistentConnection> _receiveLoadBlancer;
        private readonly ILoadBalancer<IKafkaPersistentConnection> _senderLoadBlancer;
        private readonly IAsyncPolicy _senderRetryPolicy = null;
        private readonly int _senderConfirmTimeoutMillseconds = 500;
        private readonly int _senderConfirmFlushTimeoutMillseconds = 50;
        private readonly IAsyncPolicy _receiverPolicy = null;

        private Action<EventResponse[]> _subscribeAckHandler = null;
        private Func<(EventResponse[] Messages, Exception exception), Task<bool>> _subscribeNackHandler = null;

        public EventBusKafka(
           ILoadBalancer<IKafkaPersistentConnection> receiveLoadBlancer,
           ILoadBalancer<IKafkaPersistentConnection> senderLoadBlancer,
           ILogger<IEventBus> logger,
           IServiceProvider lifetimeScope,
            int reveiverMaxDegreeOfParallelism = 10,
            int receiverAcquireRetryAttempts = 0,
            int receiverHandlerTimeoutMillseconds = 0,
            int senderRetryCount = 3,
            int senderConfirmTimeoutMillseconds=1000,
            int senderConfirmFlushTimeoutMillseconds=50)
        {

            this._reveiverMaxDegreeOfParallelism = reveiverMaxDegreeOfParallelism;
            this._receiveLoadBlancer = receiveLoadBlancer;
            this._senderLoadBlancer = senderLoadBlancer;
            this._senderConfirmTimeoutMillseconds = senderConfirmTimeoutMillseconds;
            this._senderConfirmFlushTimeoutMillseconds = senderConfirmFlushTimeoutMillseconds;
            this._lifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));

            #region 生产端策略
            this._senderRetryPolicy = Policy.NoOpAsync();//创建一个空的Policy

            this._senderRetryPolicy = _senderRetryPolicy.WrapAsync(Policy.Handle<KafkaException>()
               .Or<SocketException>()
               .Or<System.IO.IOException>()
               .WaitAndRetryAsync(senderRetryCount, retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt)), (ex, time) =>
               {
                   _logger.LogError(ex.ToString());
               }));
            #endregion

            #region 消费者策略
            _receiverPolicy = Policy.NoOpAsync();//创建一个空的Policy

            if (receiverAcquireRetryAttempts > 0)
            {
                //设置重试策略
                _receiverPolicy = _receiverPolicy.WrapAsync(Policy.Handle<Exception>()
                       .RetryAsync(receiverAcquireRetryAttempts, (ex, time) =>
                       {
                           _logger.LogError(ex, ex.ToString());
                       }));
            }

            if (receiverHandlerTimeoutMillseconds > 0)
            {
                // 设置超时
                _receiverPolicy = _receiverPolicy.WrapAsync(Policy.TimeoutAsync(
                    TimeSpan.FromMilliseconds(receiverHandlerTimeoutMillseconds),
                    TimeoutStrategy.Pessimistic,
                    (context, timespan, task) =>
                    {
                        return Task.FromResult(true);
                    }));
            }
            #endregion
        }

        #region 发送消息
        /// <summary>
        /// 发送消息
        /// </summary>
        public async Task PublishNonConfirmAsync(List<Models.EventLogEntry> Events, CancellationToken cancellationToken)
        {
            using (var tracer = new Hummingbird.Extensions.Tracing.Tracer("AMQP"))
            {
                tracer.SetComponent(_compomentName);

                Enqueue(Mapping(Events), cancellationToken);
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        public async Task<bool> PublishAsync(List<Models.EventLogEntry> Events, CancellationToken cancellationToken)
        {
            using (var tracer = new Hummingbird.Extensions.Tracing.Tracer("AMQP"))
            {
                tracer.SetComponent(_compomentName);

                Enqueue(Mapping(Events), cancellationToken);

                return await Task.FromResult(true);

            }
        }

        private List<EventMessage> Mapping(List<Models.EventLogEntry> Events)
        {
            var evtDicts = Events.Select(a => new EventMessage()
            {
                Body = a.Content,
                MessageId = a.MessageId,
                TraceId = a.TraceId,
                EventId = a.EventId,
                RouteKey = a.EventTypeName,
                Timestamp = new Timestamp(a.CreationTime, TimestampType.CreateTime),
                Headers = a.Headers ?? new Dictionary<string, object>()
            }).ToList();

            evtDicts.ForEach(message =>
            {
                if (!message.Headers.ContainsKey("x-ts"))
                {
                    //附加时间戳
                    message.Headers.Add("x-ts", message.Timestamp.UnixTimestampMs.ToString());
                }

                if (!message.Headers.ContainsKey("x-traceId"))
                {
                    message.Headers.Add("x-traceId", message.TraceId.ToString());
                }
            });

            return evtDicts;
        }

        private void Enqueue(List<EventMessage> Events, CancellationToken cancellationToken)
        {
            var persistentConnection = _senderLoadBlancer.Lease();

            try
            {

                var channel = persistentConnection.GetProducer();
                var messages = new List<Message<string, string>>();
                var topic = Events.FirstOrDefault().RouteKey;

                for (var eventIndex = 0; eventIndex < Events.Count; eventIndex++)
                {
                    using (var tracer = new Hummingbird.Extensions.Tracing.Tracer("AMQP Publish"))
                    {
                        tracer.SetComponent(_compomentName);
                        tracer.SetTag("x-eventId", Events[eventIndex].EventId);
                        tracer.SetTag("x-messageId", Events[eventIndex].MessageId);
                        tracer.SetTag("x-traceId", Events[eventIndex].TraceId);
                        _logger.LogInformation(Events[eventIndex].Body);

                        var message = new Message<string, string>();
                        message.Key = Events[eventIndex].MessageId;
                        message.Timestamp = Events[eventIndex].Timestamp;
                        message.Value = Events[eventIndex].Body;
                        message.Headers = new Headers();

                        foreach (var key in Events[eventIndex].Headers.Keys)
                        {
                            message.Headers.Add(new Header(key, UTF8Encoding.UTF8.GetBytes(Events[eventIndex].Headers[key] as string)));
                        }

                        messages.Add(message);
                    }
                }

                channel.ProduceBatch(topic, messages,TimeSpan.FromMilliseconds(_senderConfirmTimeoutMillseconds),TimeSpan.FromMilliseconds(_senderConfirmFlushTimeoutMillseconds), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw ex;
            }
        }

        #endregion

        /// <summary>
        /// 订阅消息（同一类消息可以重复订阅）
        /// 作者：郭明
        /// 日期：2017年4月3日
        /// </summary>
        /// <typeparam name="TD"></typeparam>
        /// <typeparam name="TH"></typeparam>
        /// <param name="QueueName">消息类型名称</param>        
        /// <param name="EventTypeName">消息类型名称</param>        
        /// <returns></returns>
        public IEventBus Register<TD, TH>(string QueueName, string EventTypeName = "", CancellationToken cancellationToken = default(CancellationToken))
                    where TD : class
                    where TH : IEventHandler<TD>
        {

            var persistentConnection = _receiveLoadBlancer.Lease();
            var queueName = string.IsNullOrEmpty(QueueName) ? typeof(TH).FullName : QueueName;
            var routeKey = string.IsNullOrEmpty(EventTypeName) ? typeof(TD).FullName : EventTypeName;
            var eventAction = _lifetimeScope.GetService(typeof(TH)) as IEventHandler<TD>;
            if (eventAction == null)
            {
                eventAction = System.Activator.CreateInstance(typeof(TH)) as IEventHandler<TD>;
            }

            for (int i = 0; i < _reveiverMaxDegreeOfParallelism; i++)
            {
                System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        var consumer = persistentConnection.GetConsumer();
                        consumer.Subscribe(routeKey);

                        while (true)
                        {
                            var ea = consumer.Consume(cancellationToken);

                            // 消息队列空
                            if (ea.IsPartitionEOF)
                            {
                                _logger.LogDebug("Reached end of topic {consumeResult.Topic}, partition {consumeResult.Partition}, offset {consumeResult.Offset}.");

                                continue;
                            }

                            var EventId = -1L;
                            var MessageId = ea.Key;
                            var TraceId = MessageId;

                            using (var tracer = new Hummingbird.Extensions.Tracing.Tracer("AMQP Received", TraceId))
                            { 
                                #region 获取EventId 和 TracerId
                                if (ea.Headers != null)
                                {
                                    try
                                    {
                                        long.TryParse(System.Text.Encoding.UTF8.GetString(ea.Headers.GetLastBytes("x-eventId")), out EventId);
                                    }
                                    catch
                                    { }


                                    try
                                    {
                                        TraceId = System.Text.Encoding.UTF8.GetString(ea.Headers.GetLastBytes("x-traceId"));
                                    }
                                    catch
                                    {
                                    }
                                }
                                #endregion

                                #region AMQP Received
                                try
                                {
                                    tracer.SetComponent(_compomentName);
                                    tracer.SetTag("queueName", queueName);
                                    tracer.SetTag("x-messageId", MessageId);
                                    tracer.SetTag("x-eventId", EventId);
                                    tracer.SetTag("x-traceId", TraceId);

                                    var eventResponse = new EventResponse()
                                    {
                                        EventId = EventId,
                                        MessageId = MessageId,
                                        TraceId = TraceId,
                                        Headers = new Dictionary<string, object>(),
                                        Body = default(TD),
                                        QueueName = queueName,
                                        RouteKey = routeKey,
                                        BodySource = ea.Value
                                    };

                                 
                                    try
                                    {
                                        foreach (var key in ea.Headers)
                                        {
                                            eventResponse.Headers.Add(key.Key, Encoding.UTF8.GetString(key.GetValueBytes()));
                                        }

                                        eventResponse.Body = JsonConvert.DeserializeObject<TD>(eventResponse.BodySource);
                                        _logger.LogInformation(eventResponse.BodySource);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, ex.Message);
                                    }

                                    #region AMQP ExecuteAsync
                                    using (var tracerExecuteAsync = new Hummingbird.Extensions.Tracing.Tracer("AMQP ExecuteAsync"))
                                    {
                                        var handlerSuccess = false;
                                        var handlerException = default(Exception);

                                        try
                                        {
                                            handlerSuccess = await _receiverPolicy.ExecuteAsync(async (handlerCancellationToken) =>
                                            {
                                                return await eventAction.Handle(eventResponse.Body, (Dictionary<string, object>)eventResponse.Headers, handlerCancellationToken);

                                            }, CancellationToken.None);

                                            if (handlerSuccess)
                                            {
                                                if (_subscribeAckHandler != null)
                                                {
                                                    _subscribeAckHandler(new EventResponse[] { eventResponse });
                                                }
                                                consumer.Commit(ea);

                                            }
                                            else
                                            {
                                                tracerExecuteAsync.SetError();
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            tracerExecuteAsync.SetError();
                                            handlerException = ex;
                                        }
                                        finally
                                        {
                                            if (!handlerSuccess)
                                            {
                                                //重新入队，默认：是
                                                var requeue = true;

                                                try
                                                {
                                                    //执行回调，等待业务层的处理结果
                                                    if (_subscribeNackHandler != null)
                                                    {
                                                        requeue = await _subscribeNackHandler((new EventResponse[] { eventResponse }, handlerException));
                                                    }
                                                }
                                                catch (Exception innterEx)
                                                {
                                                    _logger.LogError(innterEx, innterEx.Message);
                                                }

                                                if (!requeue)
                                                {
                                                    consumer.Commit(ea);
                                                }
                                            }
                                        }
                                    }
                                    #endregion
                                }
                                catch (Exception ex)
                                {
                                    tracer.SetError();
                                    _logger.LogError(ex.Message, ex);
                                }
                                #endregion
                            }

                        }
                      
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ex.Message);
                    }
                });
            }

            return this;
        }



        /// <summary>
        /// 订阅消息（同一类消息可以重复订阅）
        /// 作者：郭明
        /// 日期：2017年4月3日
        /// </summary>
        /// <typeparam name="TD"></typeparam>
        /// <typeparam name="TH"></typeparam>
        /// <param name="EventTypeName">消息类型名称</param>        
        /// <returns></returns>
        public IEventBus RegisterBatch<TD, TH>(string QueueName, string EventTypeName = "", int BatchSize = 50, CancellationToken cancellationToken = default(CancellationToken))
                where TD : class
                where TH : IEventBatchHandler<TD>
        {
            var persistentConnection = _receiveLoadBlancer.Lease();
            var queueName = string.IsNullOrEmpty(QueueName) ? typeof(TH).FullName : QueueName;
            var routeKey = string.IsNullOrEmpty(EventTypeName) ? typeof(TD).FullName : EventTypeName;
            var eventAction = _lifetimeScope.GetService(typeof(TH)) as IEventBatchHandler<TD>;
            if (eventAction == null)
            {
                eventAction = System.Activator.CreateInstance(typeof(TH)) as IEventBatchHandler<TD>;
            }

            for (int i = 0; i < _reveiverMaxDegreeOfParallelism; i++)
            {
                System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        var consumer = persistentConnection.GetConsumer();
                        consumer.Subscribe(routeKey);

                        while (true)
                        {
                            var handlerSuccess = false;
                            var handlerException = default(Exception);
                            var eas = consumer.ConsumeBatch(TimeSpan.FromSeconds(5), BatchSize, cancellationToken);
                            var Messages = new EventResponse[eas.Count()];

                            try
                            {
                                foreach (var ea in eas)
                                {
                                    // 消息队列空
                                    if (ea.IsPartitionEOF)
                                    {
                                        _logger.LogDebug("Reached end of topic {consumeResult.Topic}, partition {consumeResult.Partition}, offset {consumeResult.Offset}.");

                                        continue;
                                    }

                                    var EventId = -1L;
                                    var MessageId = ea.Key;
                                    var TraceId = MessageId;

                                    using (var tracer = new Hummingbird.Extensions.Tracing.Tracer("AMQP Received", TraceId))
                                    {
                                        #region 消息Header处理
                                        if (ea.Headers != null)
                                        {
                                            try
                                            {
                                                long.TryParse(System.Text.Encoding.UTF8.GetString(ea.Headers.GetLastBytes("x-eventId")), out EventId);
                                            }
                                            catch
                                            { }


                                            try
                                            {
                                                TraceId = System.Text.Encoding.UTF8.GetString(ea.Headers.GetLastBytes("x-traceId"));
                                            }
                                            catch
                                            {
                                            }
                                        }
                                        #endregion

                                        #region AMQP Received
                                        try
                                        {
                                            tracer.SetComponent(_compomentName);
                                            tracer.SetTag("queueName", queueName);
                                            tracer.SetTag("x-messageId", MessageId);
                                            tracer.SetTag("x-eventId", EventId);
                                            tracer.SetTag("x-traceId", TraceId);

                                            var eventResponse = new EventResponse()
                                            {
                                                EventId = EventId,
                                                MessageId = MessageId,
                                                TraceId = TraceId,
                                                Headers = new Dictionary<string, object>(),
                                                Body = default(TD),
                                                QueueName = queueName,
                                                RouteKey = routeKey,
                                                BodySource = ea.Value
                                            };


                                            try
                                            {
                                                foreach (var key in ea.Headers)
                                                {
                                                    eventResponse.Headers.Add(key.Key, Encoding.UTF8.GetString(key.GetValueBytes()));
                                                }

                                                eventResponse.Body = JsonConvert.DeserializeObject<TD>(eventResponse.BodySource);
                                                _logger.LogInformation(eventResponse.BodySource);
                                            }
                                            catch (Exception ex)
                                            {
                                                _logger.LogError(ex, ex.Message);
                                            }


                                        }
                                        catch (Exception ex)
                                        {
                                            tracer.SetError();
                                            _logger.LogError(ex.Message, ex);
                                        }
                                        #endregion
                                    }
                                }

                                if (Messages != null && Messages.Any())
                                {
                                    using (var executeTracer = new Hummingbird.Extensions.Tracing.Tracer("AMQP Execute"))
                                    {
                                        executeTracer.SetComponent(_compomentName);

                                        handlerSuccess = await _receiverPolicy.ExecuteAsync(async (handlerCancellationToken) =>
                                        {
                                            return await eventAction.Handle(Messages.Select(a => (TD)a.Body).ToArray(), Messages.Select(a => (Dictionary<string, object>)a.Headers).ToArray(), handlerCancellationToken);

                                        }, cancellationToken);

                                        if (handlerSuccess)
                                        {
                                            #region 消息处理成功
                                            if (_subscribeAckHandler != null && Messages.Length > 0)
                                            {
                                                _subscribeAckHandler(Messages);
                                            }

                                            consumer.Commit();

                                            #endregion
                                        }
                                        else
                                        {
                                            executeTracer.SetError();
                                        }
                                    }
                                }
                            }
                            catch(Exception ex)
                            {
                                _logger.LogError(ex, ex.Message);

                            }
                            finally
                            {
                                if (!handlerSuccess)
                                {
                                    //重新入队，默认：是
                                    var requeue = true;

                                    try
                                    {
                                        //执行回调，等待业务层的处理结果
                                        if (_subscribeNackHandler != null)
                                        {
                                            requeue = await _subscribeNackHandler((Messages, handlerException));
                                        }
                                    }
                                    catch (Exception innterEx)
                                    {
                                        _logger.LogError(innterEx, innterEx.Message);
                                    }

                                    if (!requeue)
                                    {
                                        consumer.Commit();
                                    }
                                }
                            }
                        
                            
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ex.Message);
                    }
                });
            }

            return this;
        }


        /// <summary>
        /// 订阅消息
        /// </summary>
        /// <param name="ackHandler"></param>
        /// <param name="nackHandler"></param>
        /// <returns></returns>
        public IEventBus Subscribe(
         Action<EventResponse[]> ackHandler,
         Func<(EventResponse[] Messages, Exception Exception), Task<bool>> nackHandler)
        {
            _subscribeAckHandler = ackHandler;
            _subscribeNackHandler = nackHandler;
            return this;
        }
    }
}
