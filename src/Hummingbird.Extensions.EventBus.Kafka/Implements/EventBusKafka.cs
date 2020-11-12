using Confluent.Kafka;
using Hummingbird.Extensions.EventBus.Abstractions;
using Hummingbird.Extensions.EventBus.Models;
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
using Hummingbird.Extensions.EventBus.Kafka.Extersions;

namespace Hummingbird.Extensions.EventBus.Kafka
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
            int receiverAcquireRetryAttempts = 0,
            int receiverHandlerTimeoutMillseconds = 0,
            int senderRetryCount = 3,
            int senderConfirmTimeoutMillseconds = 1000,
            int senderConfirmFlushTimeoutMillseconds = 50)
        {

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
                var groups = Events.GroupBy(a => a.RouteKey).Select(a => a.Key);

                foreach (var topic in groups)
                {
                    var messages = new List<Message<string, string>>();
                    var curEvents = Events.Where(a => a.RouteKey == topic).ToArray();
                    for (var eventIndex = 0; eventIndex < curEvents.Count(); eventIndex++)
                    {
                        using (var tracer = new Hummingbird.Extensions.Tracing.Tracer("AMQP Publish"))
                        {
                            tracer.SetComponent(_compomentName);
                            tracer.SetTag("x-eventId", curEvents[eventIndex].EventId);
                            tracer.SetTag("x-messageId", curEvents[eventIndex].MessageId);
                            tracer.SetTag("x-traceId", curEvents[eventIndex].TraceId);
                            _logger.LogInformation(curEvents[eventIndex].Body);

                            var message = new Message<string, string>();
                            message.Key = curEvents[eventIndex].MessageId;
                            message.Timestamp = curEvents[eventIndex].Timestamp;
                            message.Value = curEvents[eventIndex].Body;
                            message.Headers = new Headers();

                            foreach (var key in curEvents[eventIndex].Headers.Keys)
                            {
                                message.Headers.Add(new Header(key, UTF8Encoding.UTF8.GetBytes(curEvents[eventIndex].Headers[key] as string)));
                            }

                            messages.Add(message);
                        }
                    }

                    channel.ProduceBatch(topic, messages, TimeSpan.FromMilliseconds(_senderConfirmTimeoutMillseconds), TimeSpan.FromMilliseconds(_senderConfirmFlushTimeoutMillseconds), cancellationToken);
                }
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


            System.Threading.Tasks.Task.Run(async () =>
            {
                IConsumer<string, string> consumer = null;

                try
                {
                    consumer = persistentConnection.GetConsumer();
                    consumer.Subscribe(routeKey);

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            var ea = consumer.Consume(cancellationToken);

                            // 消息队列空
                            if (ea != null && ea.IsPartitionEOF)
                            {
                                _logger.LogDebug("Reached end of topic {consumeResult.Topic}, partition {consumeResult.Partition}, offset {consumeResult.Offset}.");

                                continue;
                            }
                            else
                            {
                                _logger.LogInformation($"Consumed message '{ea.Value}' at: '{ea.TopicPartitionOffset}'.");
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

                                tracer.SetComponent(_compomentName);
                                tracer.SetTag("queueName", queueName);
                                tracer.SetTag("x-messageId", MessageId);
                                tracer.SetTag("x-eventId", EventId);
                                tracer.SetTag("x-traceId", TraceId);

                                try
                                {
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


                                    #region 格式化消息
                                    try
                                    {
                                        #region 设置body
                                        eventResponse.Body = JsonConvert.DeserializeObject<TD>(eventResponse.BodySource);
                                        #endregion

                                        #region 设置header
                                        foreach (var key in ea.Headers)
                                        {
                                            eventResponse.Headers.Add(key.Key, Encoding.UTF8.GetString(key.GetValueBytes()));
                                        }

                                        if (!eventResponse.Headers.ContainsKey("x-topic"))
                                        {
                                            eventResponse.Headers.Add("x-topic", routeKey);
                                        }
                                        if (!eventResponse.Headers.ContainsKey("x-messageId"))
                                        {
                                            eventResponse.Headers.Add("x-messageId", MessageId);
                                        }
                                        if (!eventResponse.Headers.ContainsKey("x-eventId"))
                                        {
                                            eventResponse.Headers.Add("x-eventId", EventId);
                                        }
                                        if (!eventResponse.Headers.ContainsKey("x-traceId"))
                                        {
                                            eventResponse.Headers.Add("x-traceId", TraceId);
                                        }
                                        #endregion

                                        _logger.LogDebug(eventResponse.BodySource);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, ex.Message);
                                    }
                                    #endregion

                                    #region 处理消息
                                    using (var tracerExecuteAsync = new Hummingbird.Extensions.Tracing.Tracer("AMQP Execute"))
                                    {
                                        var handlerSuccess = false;
                                        var handlerException = default(Exception);

                                        tracerExecuteAsync.SetComponent(_compomentName);
                                        tracerExecuteAsync.SetTag("queueName", queueName);
                                        tracerExecuteAsync.SetTag("x-messageId", MessageId);
                                        tracerExecuteAsync.SetTag("x-eventId", EventId);
                                        tracerExecuteAsync.SetTag("x-traceId", TraceId);

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
                                                else
                                                {
                                                    consumer.Seek(ea.TopicPartitionOffset); //重新入队重试
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
                            }

                        }
                        catch (ConsumeException ex)
                        {
                            _logger.LogError(ex, ex.Message);

                            consumer.Seek(ex.ConsumerRecord.TopicPartitionOffset); //重新入队重试                           
                        }
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);

                    if (consumer != null)
                    {
                        consumer.Close();
                    }
                }

            });

            return this;
        }




        /// <summary>
        /// 订阅消息（同一类消息可以重复订阅）
        /// 作者：郭明
        /// 日期：2017年4月3日
        /// </summary>
        /// <typeparam name="TD"></typeparam>
        /// <typeparam name="TH"></typeparam>
        /// <param name="QueueName"></param>
        /// <param name="EventTypeName"></param>
        /// <param name="BatchSize"></param>
        /// <param name="cancellationToken"></param>
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

            System.Threading.Tasks.Task.Run(async () =>
            {
                IConsumer<string, string> consumer = null;

                try
                {
                    consumer = persistentConnection.GetConsumer();
                    consumer.Subscribe(routeKey);

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            var handlerSuccess = false;
                            var handlerException = default(Exception);
                            var eas = consumer.ConsumeBatch(TimeSpan.FromSeconds(5), BatchSize, cancellationToken).ToArray();
                            var Messages = new EventResponse[eas.Count()];

                            if (Messages.Length > 0)
                            {
                                _logger.LogInformation($"Consumed message '{eas.LastOrDefault().Value}' at: '{eas.LastOrDefault().TopicPartitionOffset}'.");
                            }
                            else
                            {
                                continue;
                            }

                            try
                            {
                                #region 批量格式化消息
                                for (int j = 0; j < eas.Length; j++)
                                {
                                    var ea = eas[j];

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
                                        #region 获取EventId & TraceId
                                        if (ea.Headers != null && ea.Headers.Count>0)
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

                                        tracer.SetComponent(_compomentName);
                                        tracer.SetTag("queueName", queueName);
                                        tracer.SetTag("x-messageId", MessageId);
                                        tracer.SetTag("x-eventId", EventId);
                                        tracer.SetTag("x-traceId", TraceId);

                                        Messages[j] = new EventResponse()
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
                                            #region 设置Body
                                            Messages[j].Body = JsonConvert.DeserializeObject<TD>(Messages[j].BodySource);
                                            #endregion

                                            #region 设置header
                                            foreach (var key in ea.Headers)
                                            {
                                                Messages[j].Headers.Add(key.Key, Encoding.UTF8.GetString(key.GetValueBytes()));
                                            }

                                            if (!Messages[j].Headers.ContainsKey("x-topic"))
                                            {
                                                Messages[j].Headers.Add("x-topic", routeKey);
                                            }
                                            if (!Messages[j].Headers.ContainsKey("x-messageId"))
                                            {
                                                Messages[j].Headers.Add("x-messageId", MessageId);
                                            }
                                            if (!Messages[j].Headers.ContainsKey("x-eventId"))
                                            {
                                                Messages[j].Headers.Add("x-eventId", EventId);
                                            }
                                            if (!Messages[j].Headers.ContainsKey("x-traceId"))
                                            {
                                                Messages[j].Headers.Add("x-traceId", TraceId);
                                            }
                                            #endregion

                                            _logger.LogDebug(Messages[j].BodySource);

                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogError(ex, ex.Message);
                                        }
                                    }
                                }
                                #endregion

                                #region 批量处理消息

                                if (Messages != null && Messages.Any())
                                {
                                    using (var executeTracer = new Hummingbird.Extensions.Tracing.Tracer("AMQP Execute"))
                                    {
                                        executeTracer.SetComponent(_compomentName);
                                        executeTracer.SetTag("queueName", queueName);

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
                                #endregion
                            }
                            catch (Exception ex)
                            {
                                handlerException = ex;
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
                                        if (_subscribeNackHandler != null && Messages != null && Messages.Any())
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
                                    else
                                    {
                                        if (eas.Length > 0)
                                        {
                                            consumer.Seek(eas.FirstOrDefault().TopicPartitionOffset);
                                        }
                                    }
                                }
                            }
                        }
                        catch (ConsumeException ex)
                        {
                            _logger.LogError(ex, ex.Message);

                            consumer.Seek(ex.ConsumerRecord.TopicPartitionOffset);
                        }
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);

                    if (consumer != null)
                    {
                        consumer.Close();
                    }
                }
            });


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
