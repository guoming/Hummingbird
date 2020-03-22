using Hummingbird.Extersions.EventBus.Abstractions;
using Hummingbird.Extersions.EventBus.Models;
using Hummingbird.LoadBalancers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hummingbird.Extersions.EventBus.RabbitMQ
{

    /// <summary>
    /// 消息队列
    /// 作者：郭明
    /// 日期：2017年4月5日
    /// </summary>
    public class EventBusRabbitMQ : IEventBus
    {
        public struct EventMessage
        {
            public long EventId { get; set; }

            public string MessageId { get; set; }

            public string Body { get; set; }

            public string RouteKey { get; set; }

            public long Timestamp { get; set; }

            public IDictionary<string,object> Headers { get; set; }
        
        }


        private readonly string _exchange = "amq.topic";
        private readonly string _exchangeType = "topic";
        private readonly ushort _preFetch = 1;
        private readonly string _compomentName = typeof(EventBusRabbitMQ).FullName;
        private readonly ILogger<IEventBus> _logger;
        private readonly IServiceProvider _lifetimeScope;

        private readonly ILoadBalancer<IRabbitMQPersistentConnection> _receiverLoadBlancer;
        private readonly int _reveiverMaxDegreeOfParallelism;
        private readonly IAsyncPolicy _receiverPolicy = null;


        private readonly ILoadBalancer<IRabbitMQPersistentConnection> _senderLoadBlancer;
        private readonly int _senderConfirmTimeoutMillseconds;
        private readonly IAsyncPolicy _senderRetryPolicy = null;

        private Action<EventResponse[]> _subscribeAckHandler = null;
        private Func<(EventResponse[] Messages, Exception exception), Task<bool>> _subscribeNackHandler = null;

        public EventBusRabbitMQ(
           ILoadBalancer<IRabbitMQPersistentConnection> receiveLoadBlancer,
           ILoadBalancer<IRabbitMQPersistentConnection> senderLoadBlancer,
           ILogger<IEventBus> logger,
           IServiceProvider lifetimeScope,
            int reveiverMaxDegreeOfParallelism = 10,
            int receiverAcquireRetryAttempts = 0,
            int receiverHandlerTimeoutMillseconds = 0,
            int senderRetryCount = 3,
            int senderConfirmTimeoutMillseconds=500,
            ushort preFetch = 1,
            string exchange = "amp.topic",
            string exchangeType = "topic")
        {
            this._preFetch = preFetch;
            this._exchange = exchange;
            this._exchangeType = exchangeType;


            this._reveiverMaxDegreeOfParallelism = reveiverMaxDegreeOfParallelism;
            this._receiverLoadBlancer = receiveLoadBlancer;

            this._senderLoadBlancer = senderLoadBlancer;
            this._senderConfirmTimeoutMillseconds = senderConfirmTimeoutMillseconds;

            this._lifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));

            #region 生产端策略
            this._senderRetryPolicy = Policy.NoOpAsync();//创建一个空的Policy

            this._senderRetryPolicy = _senderRetryPolicy.WrapAsync(Policy.Handle<BrokerUnreachableException>()
               .Or<SocketException>()
               .Or<System.IO.IOException>()
               .Or<AlreadyClosedException>()
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
                    TimeSpan.FromSeconds(receiverHandlerTimeoutMillseconds),
                    TimeoutStrategy.Pessimistic,
                    (context, timespan, task) =>
                    {
                        return Task.FromResult(true);
                    }));
            }
            #endregion
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        public async Task PublishNonConfirmAsync(List<Models.EventLogEntry> Events,CancellationToken cancellationToken=default(CancellationToken))
        {
            using (var tracer = new Hummingbird.Extensions.Tracing.Tracer("AMQP"))
            {
                tracer.SetComponent(_compomentName);

                await Enqueue(Mapping(Events),false,cancellationToken);

            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        public async Task<bool> PublishAsync(List<Models.EventLogEntry> Events, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var tracer = new Hummingbird.Extensions.Tracing.Tracer("AMQP"))
            {
                tracer.SetComponent(_compomentName);
                return await Enqueue(Mapping(Events),true,cancellationToken);

            }
        }

        private List<EventMessage> Mapping(List<Models.EventLogEntry> Events)
        {
            var evtDicts = Events.Select(a => new EventMessage()
            {
                Body = a.Content,
                MessageId = a.MessageId.ToString(),
                EventId = a.EventId,
                RouteKey = a.EventTypeName,
                Timestamp=a.CreationTime.ToTimestamp(),
                Headers = a.Headers ?? new Dictionary<string, object>()
            }).ToList();

            evtDicts.ForEach(message =>
            {
                if (!message.Headers.ContainsKey("x-ts"))
                {
                    //附加时间戳
                    message.Headers.Add("x-ts", message.Timestamp);
                }
            });

            return evtDicts;
        }


        private  async Task<bool> Enqueue(List<EventMessage> Events,bool confirm, CancellationToken cancellationToken = default(CancellationToken))
        {
           
                var persistentConnection = _senderLoadBlancer.Lease();

                try
                {
                    if (!persistentConnection.IsConnected)
                    {
                        persistentConnection.TryConnect();
                    }

                    var channel = persistentConnection.GetProducer();

                    // 提交走批量通道
                    var batchPublish = channel.CreateBasicPublishBatch();

                    for (var eventIndex = 0; eventIndex < Events.Count; eventIndex++)
                    {
                   
                        await _senderRetryPolicy.ExecuteAsync((ct) =>
                        {
                            var MessageId = Events[eventIndex].MessageId;
                            var json = Events[eventIndex].Body;
                            var routeKey = Events[eventIndex].RouteKey;
                            byte[] bytes = Encoding.UTF8.GetBytes(json);
                            //设置消息持久化
                            IBasicProperties properties = channel.CreateBasicProperties();
                            properties.DeliveryMode = 2;
                            properties.MessageId = MessageId;
                            properties.Headers = new Dictionary<string, Object>();
                            properties.Headers["x-eventId"] = Events[eventIndex].EventId;

                            using (var tracer = new Hummingbird.Extensions.Tracing.Tracer("AMQP Publish"))
                            {
                                tracer.SetComponent(_compomentName);
                                tracer.SetTag("x-messageId", MessageId);
                                tracer.SetTag("x-eventId", Events[eventIndex].EventId);
                                _logger.LogInformation(json);

                                foreach (var key in Events[eventIndex].Headers.Keys)
                                {
                                    if (!properties.Headers.ContainsKey(key))
                                    {
                                        properties.Headers.Add(key, Events[eventIndex].Headers[key]);
                                    }
                                }

                                if (Events[eventIndex].Headers.ContainsKey("x-first-death-queue"))
                                {
                                    //延时队列或者直接写死信的情况
                                    var newQueue = Events[eventIndex].Headers["x-first-death-queue"].ToString();

                                    //创建一个队列                         
                                    channel.QueueDeclare(
                                                        queue: newQueue,
                                                        durable: true,
                                                        exclusive: false,
                                                        autoDelete: false,
                                                        arguments: Events[eventIndex].Headers);

                                    batchPublish.Add(
                                            exchange: "",
                                            mandatory: true,
                                            routingKey: newQueue,
                                            properties: properties,
                                            body: bytes);
                                }
                                else
                                {
                                    //发送到正常队列
                                    batchPublish.Add(
                                                    exchange: _exchange,
                                                    mandatory: true,
                                                    routingKey: routeKey,
                                                    properties: properties,
                                                    body: bytes);
                                }
                            }

                            return Task.FromResult(true);

                        }, cancellationToken);
                    }

                    //批量提交
                    batchPublish.Publish();

                    if (confirm)
                    {
                        return channel.WaitForConfirms(TimeSpan.FromMilliseconds(_senderConfirmTimeoutMillseconds));
                    }
                    else
                    {
                        return true;
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    throw ex;
                }
          
        }

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
            var queueName = string.IsNullOrEmpty(QueueName) ? typeof(TH).FullName : QueueName;
            var routeKey = string.IsNullOrEmpty(EventTypeName) ? typeof(TD).FullName : EventTypeName;
            var eventAction = _lifetimeScope.GetService(typeof(TH)) as IEventHandler<TD>;
            if (eventAction == null)
            {
                eventAction = System.Activator.CreateInstance(typeof(TH)) as IEventHandler<TD>;
            }
            var persistentConnection = _receiverLoadBlancer.Lease();

            if (!persistentConnection.IsConnected)
            {
                persistentConnection.TryConnect();
            }

            for (int i = 0; i < _reveiverMaxDegreeOfParallelism; i++)
            {
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {

                        var _channel = persistentConnection.GetConsumer();

                        //direct fanout topic  
                        _channel.ExchangeDeclare(_exchange, _exchangeType, true, false, null);

                        //在MQ上定义一个持久化队列，如果名称相同不会重复创建
                        _channel.QueueDeclare(queueName, true, false, false, null);
                        //绑定交换器和队列
                        _channel.QueueBind(queueName, _exchange, routeKey);
                        //绑定交换器和队列
                        _channel.QueueBind(queueName, _exchange, queueName);
                        //输入1，那如果接收一个消息，但是没有应答，则客户端不会收到下一个消息
                        _channel.BasicQos(0, _preFetch, false);
                        //在队列上定义一个消费者a
                        EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);

                        consumer.Received += async (ch, ea) =>
                        {
                            using (var tracer = new Hummingbird.Extensions.Tracing.Tracer("AMQP Received"))
                            {
                                tracer.SetComponent(_compomentName);
                                tracer.SetTag("x-messageId", ea.BasicProperties.MessageId);
                                tracer.SetTag("queueName", queueName);

                                #region AMQP Received
                                try
                                {
                                    #region Ensure IsConnected
                                    if (!persistentConnection.IsConnected)
                                    {
                                        persistentConnection.TryConnect();
                                    }
                                    #endregion

                                    long EventId = -1;
                                    if (ea.BasicProperties.Headers != null && ea.BasicProperties.Headers.ContainsKey("x-eventId"))
                                    {
                                        long.TryParse(ea.BasicProperties.Headers["x-eventId"].ToString(), out EventId);

                                        tracer.SetTag("x-eventId", EventId);
                                    }

                                    var eventResponse = new EventResponse()
                                    {
                                        EventId = EventId,
                                        MessageId = string.IsNullOrEmpty(ea.BasicProperties.MessageId) ? Guid.NewGuid().ToString("N") : ea.BasicProperties.MessageId,
                                        Headers = ea.BasicProperties.Headers ?? new Dictionary<string, object>(),
                                        Body = default(TD),
                                        QueueName = queueName,
                                        RouteKey = routeKey,
                                        BodySource = Encoding.UTF8.GetString(ea.Body)
                                    };

                                    try
                                    {
                                        eventResponse.Body = JsonConvert.DeserializeObject<TD>(eventResponse.BodySource);

                                        if (!eventResponse.Headers.ContainsKey("x-exchange"))
                                        {
                                            eventResponse.Headers.Add("x-exchange", _exchange);
                                        }

                                        if (!eventResponse.Headers.ContainsKey("x-exchange-type"))
                                        {
                                            eventResponse.Headers.Add("x-exchange-type", _exchangeType);
                                        }

                                        _logger.LogInformation(eventResponse.BodySource);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, ex.Message);
                                    }

                                    #region AMQP ExecuteAsync
                                    using (var tracerExecuteAsync = new Hummingbird.Extensions.Tracing.Tracer("AMQP Execute"))
                                    {
                                        var handlerSuccess = false;
                                        var handlerException = default(Exception);

                                        try
                                        {
                                            handlerSuccess = await _receiverPolicy.ExecuteAsync(async (handlerCancellationToken) =>
                                           {
                                               return await eventAction.Handle(eventResponse.Body, (Dictionary<string, object>)eventResponse.Headers, handlerCancellationToken);

                                           }, cancellationToken);

                                            if (handlerSuccess)
                                            {
                                                if (_subscribeAckHandler != null)
                                                {
                                                    _subscribeAckHandler(new EventResponse[] { eventResponse });
                                                }

                                                //确认消息
                                                _channel.BasicAck(ea.DeliveryTag, false);

                                            }
                                            else
                                            {
                                                tracerExecuteAsync.SetError();
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogError(ex, ex.Message);
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

                                                //确认消息
                                                _channel.BasicReject(ea.DeliveryTag, requeue);
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
                        };

                        consumer.Unregistered += (ch, ea) =>
                        {
                            _logger.LogDebug($"MQ:{queueName} Consumer_Unregistered");
                        };

                        consumer.Registered += (ch, ea) =>
                        {
                            _logger.LogDebug($"MQ:{queueName} Consumer_Registered");
                        };

                        consumer.Shutdown += (ch, ea) =>
                        {
                            _logger.LogDebug($"MQ:{queueName} Consumer_Shutdown.{ea.ReplyText}");
                        };

                        consumer.ConsumerCancelled += (object sender, ConsumerEventArgs e) =>
                        {
                            _logger.LogDebug($"MQ:{queueName} ConsumerCancelled");
                        };

                        //消费队列，并设置应答模式为程序主动应答
                        _channel.BasicConsume(queueName, false, consumer);
                    }
                    catch(Exception ex)
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
            var queueName = string.IsNullOrEmpty(QueueName) ? typeof(TH).FullName : QueueName;
            var routeKey = string.IsNullOrEmpty(EventTypeName) ? typeof(TD).FullName : EventTypeName;
            var eventAction = _lifetimeScope.GetService(typeof(TH)) as IEventBatchHandler<TD>;

            if (eventAction == null)
            {
                eventAction = System.Activator.CreateInstance(typeof(TH)) as IEventBatchHandler<TD>;
            }
            var persistentConnection = _receiverLoadBlancer.Lease();

            if (!persistentConnection.IsConnected)
            {
                persistentConnection.TryConnect();
            }

            for (int parallelism = 0; parallelism < _reveiverMaxDegreeOfParallelism; parallelism++)
            {
                try
                {
                    var _channel = persistentConnection.GetConsumer();
                  

                    //direct fanout topic  
                    _channel.ExchangeDeclare(_exchange, _exchangeType, true, false, null);
                    //在MQ上定义一个持久化队列，如果名称相同不会重复创建
                    _channel.QueueDeclare(queueName, true, false, false, null);
                    //绑定交换器和队列
                    _channel.QueueBind(queueName, _exchange, routeKey);
                    _channel.QueueBind(queueName, _exchange, queueName);
                    //输入1，那如果接收一个消息，但是没有应答，则客户端不会收到下一个消息
                    _channel.BasicQos(0, (ushort)BatchSize, false);

                    Task.Run(async () =>
                    {
                        while (true)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            try
                            {
                                var batchPool = new List<(string MessageId, BasicGetResult ea)>();
                                var batchLastDeliveryTag = 0UL;

                                #region batch Pull
                                for (var i = 0; i < BatchSize; i++)
                                {
                                    var ea = _channel.BasicGet(queueName, false);

                                    if (ea != null)
                                    {
                                        var MessageId = ea.BasicProperties.MessageId;

                                        if (string.IsNullOrEmpty(MessageId))
                                        {
                                            batchPool.Add((Guid.NewGuid().ToString("N"), ea));
                                        }
                                        else
                                        {
                                            batchPool.Add((ea.BasicProperties.MessageId, ea));
                                        }

                                        batchLastDeliveryTag = ea.DeliveryTag;
                                    }
                                    else
                                    {
                                        break;
                                    }

                                }

                                #endregion

                                //队列不为空
                                if (batchPool.Count > 0)
                                {
                                    using (var receiveTracer = new Hummingbird.Extensions.Tracing.Tracer("AMQP Received"))
                                    {
                                        var basicGetResults = batchPool.Select(a => a.ea).ToArray();
                                        var Messages = new EventResponse[basicGetResults.Length];
                                        var handlerSuccess = false;
                                        var handlerException = default(Exception);

                                        try
                                        {
                                            for (int i = 0; i < basicGetResults.Length; i++)
                                            {
                                                var ea = basicGetResults[i];

                                                Messages[i] = new EventResponse()
                                                {
                                                    EventId = -1,
                                                    MessageId = string.IsNullOrEmpty(ea.BasicProperties.MessageId) ? Guid.NewGuid().ToString("N") : ea.BasicProperties.MessageId,
                                                    Headers = ea.BasicProperties.Headers ?? new Dictionary<string, object>(),
                                                    Body = default(TD),
                                                    RouteKey = routeKey,
                                                    QueueName = queueName,
                                                    BodySource = Encoding.UTF8.GetString(ea.Body)
                                                };

                                                using (var tracer = new Hummingbird.Extensions.Tracing.Tracer("AMQP BasicGet"))
                                                {
                                                    tracer.SetComponent(_compomentName);
                                                    tracer.SetTag("queueName", queueName);
                                                    tracer.SetTag("x-messageId", Messages[i].MessageId);
                                                    tracer.SetTag("x-eventId", Messages[i].EventId);
                                                    try
                                                    {

                                                        Messages[i].Body = JsonConvert.DeserializeObject<TD>(Messages[i].BodySource);
                                                        _logger.LogInformation(Messages[i].BodySource);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        tracer.SetError();
                                                        _logger.LogError(ex, ex.Message);
                                                    }
                                                }
                                            }

                                            for (int i = 0; i < Messages.Length; i++)
                                            {
                                                if (Messages[i].Headers.ContainsKey("x-eventId") && long.TryParse(Messages[i].Headers["x-eventId"].ToString(), out long EventId))
                                                {
                                                    Messages[i].EventId = EventId;
                                                }

                                                if (!Messages[i].Headers.ContainsKey("x-exchange"))
                                                {
                                                    Messages[i].Headers.Add("x-exchange", _exchange);
                                                }

                                                if (!Messages[i].Headers.ContainsKey("x-exchange-type"))
                                                {
                                                    Messages[i].Headers.Add("x-exchange-type", _exchangeType);
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

                                                        //确认消息被处理
                                                        _channel.BasicAck(batchLastDeliveryTag, true);

                                                        #endregion
                                                    }
                                                    else
                                                    {
                                                        executeTracer.SetError();
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogError(ex, ex.Message);
                                            receiveTracer.SetError();
                                            handlerException = ex;
                                        }
                                        finally
                                        {
                                            if (!handlerSuccess)
                                            {
                                                #region 消息处理失败
                                                var requeue = true;
                                                try
                                                {
                                                    if (_subscribeNackHandler != null && Messages.Length > 0)
                                                    {
                                                        requeue = await _subscribeNackHandler((Messages, handlerException));
                                                    }
                                                }
                                                catch (Exception innterEx)
                                                {
                                                    _logger.LogError(innterEx.Message, innterEx);
                                                }

                                                _channel.BasicNack(batchLastDeliveryTag, true, requeue);

                                                #endregion
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex.Message, ex);
                            }

                            System.Threading.Thread.Sleep(1);
                        }
                    });

                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
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
