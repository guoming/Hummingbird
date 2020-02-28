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

            public string TraceId { get; set; }

            public string Body { get; set; }

            public string RouteKey { get; set; }

            public IDictionary<string,object> Headers { get; set; }
        
        }

        private readonly IServiceProvider _lifetimeScope;
        private readonly ILoadBalancer<IRabbitMQPersistentConnection> _receiveLoadBlancer;
        private readonly ILoadBalancer<IRabbitMQPersistentConnection> _senderLoadBlancer;
        private readonly ILogger<IEventBus> _logger;
        private readonly string _exchange = "amq.topic";
        private readonly string _exchangeType = "topic";
        private readonly ushort _preFetch = 1;
        private readonly int _IdempotencyDuration;
        private readonly int _reveiverMaxDegreeOfParallelism;
        private readonly string _compomentName = typeof(EventBusRabbitMQ).FullName;

        private Action<EventResponse[]> _subscribeAckHandler = null;
        private Func<(EventResponse[] Messages, Exception exception), Task<bool>> _subscribeNackHandler = null;
        private static List<IModel> _subscribeChannels = new List<IModel>();
        private readonly SemaphoreSlim readLock = new SemaphoreSlim(1, 1);
        private static ConcurrentDictionary<string, SortedList<string, bool>> _channelAllReturnMessageIds = new ConcurrentDictionary<string, SortedList<string, bool>>();
        private static ConcurrentDictionary<string, SortedList<ulong, string>> _channelAllUnconfirmMessageIds = new ConcurrentDictionary<string, SortedList<ulong, string>>();

        private readonly RetryPolicy _eventBusSenderRetryPolicy = null;
        private readonly IAsyncPolicy _eventBusReceiverPolicy = null;

        public EventBusRabbitMQ(
           ILoadBalancer<IRabbitMQPersistentConnection> receiveLoadBlancer,
           ILoadBalancer<IRabbitMQPersistentConnection> senderLoadBlancer,
           ILogger<IEventBus> logger,
           IServiceProvider lifetimeScope,
            int reveiverMaxDegreeOfParallelism = 10,
            int receiverAcquireRetryAttempts = 0,
            int receiverHandlerTimeoutMillseconds = 0,
            int senderRetryCount = 3,
            ushort preFetch = 1,
            string exchange = "amp.topic",
            string exchangeType = "topic")
        {

            this._reveiverMaxDegreeOfParallelism = reveiverMaxDegreeOfParallelism;
            this._receiveLoadBlancer = receiveLoadBlancer;
            this._senderLoadBlancer = senderLoadBlancer;
            this._lifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._preFetch = preFetch;
            this._exchange = exchange;
            this._exchangeType = exchangeType;

            #region 生产端策略
            this._eventBusSenderRetryPolicy = RetryPolicy.Handle<BrokerUnreachableException>()
               .Or<SocketException>()
               .Or<System.IO.IOException>()
               .Or<AlreadyClosedException>()
               .WaitAndRetry(senderRetryCount, retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt)), (ex, time) =>
               {
                   _logger.LogError(ex.ToString());
               });
            #endregion


            #region 消费者策略
            _eventBusReceiverPolicy = Policy.NoOpAsync();//创建一个空的Policy

            if (receiverAcquireRetryAttempts > 0)
            {
                //设置重试策略
                _eventBusReceiverPolicy = _eventBusReceiverPolicy.WrapAsync(Policy.Handle<Exception>()
                       .RetryAsync(receiverAcquireRetryAttempts, (ex, time) =>
                       {
                           _logger.LogError(ex, ex.ToString());
                       }));
            }

            if (receiverHandlerTimeoutMillseconds > 0)
            {
                // 设置超时
                _eventBusReceiverPolicy = _eventBusReceiverPolicy.WrapAsync(Policy.TimeoutAsync(
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
        public async Task PublishNonConfirmAsync(List<Models.EventLogEntry> Events)
        {
            using (var tracer = new Hummingbird.Extensions.Tracing.Tracer("AMQP"))
            {
                tracer.SetComponent(_compomentName);

                var evtDicts = Events.Select(a => new EventMessage()
                {
                    Body = a.Content,
                    MessageId = a.MessageId,
                    TraceId = a.TraceId,
                    EventId = a.EventId,
                    RouteKey = a.EventTypeName,
                    Headers = a.Headers ?? new Dictionary<string, object>()
                }).ToList();

                evtDicts.ForEach(message =>
                {
                    if (!message.Headers.ContainsKey("x-ts"))
                    {
                        //附加时间戳
                        message.Headers.Add("x-ts", DateTime.UtcNow.ToTimestamp());

                    }

                    if (!message.Headers.ContainsKey("x-traceId"))
                    {
                        message.Headers.Add("x-traceId", message.TraceId);
                    }
                });

                await EnqueueNoConfirm(evtDicts);
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        public async Task<bool> PublishAsync(List<Models.EventLogEntry> Events)
        {
            using (var tracer = new Hummingbird.Extensions.Tracing.Tracer("AMQP"))
            {
                tracer.SetComponent(_compomentName);

                var evtDicts = Events.Select(a => new EventMessage()
                {
                    Body = a.Content,
                    MessageId = a.MessageId,
                    TraceId = a.TraceId,
                    EventId = a.EventId,
                    RouteKey = a.EventTypeName,
                    Headers = a.Headers ?? new Dictionary<string, object>()
                }).ToList();


                evtDicts.ForEach(message =>
                {
                    if (!message.Headers.ContainsKey("x-ts"))
                    {

                        //附加时间戳
                        message.Headers.Add("x-ts", DateTime.UtcNow.ToTimestamp());
                    }

                    if (!message.Headers.ContainsKey("x-traceId"))
                    {
                        message.Headers.Add("x-traceId", message.TraceId);
                    }
                });

                return await EnqueueConfirm(evtDicts);

            }
        }

        async Task EnqueueNoConfirm(List<EventMessage> Events)
        {
            var persistentConnection = _senderLoadBlancer.Lease();

            try
            {

                if (!persistentConnection.IsConnected)
                {
                    persistentConnection.TryConnect();
                }

                var _channel = persistentConnection.GetModel();

                // 提交走批量通道
                var _batchPublish = _channel.CreateBasicPublishBatch();

                for (var eventIndex = 0; eventIndex < Events.Count; eventIndex++)
                {
                    using (var tracer = new Hummingbird.Extensions.Tracing.Tracer("AMQP Publish"))
                    {
                        var json = Events[eventIndex].Body;
                        var routeKey = Events[eventIndex].RouteKey;
                        byte[] bytes = Encoding.UTF8.GetBytes(json);

                        //设置消息持久化
                        IBasicProperties properties = _channel.CreateBasicProperties();
                        properties.DeliveryMode = 2;
                        properties.MessageId = Events[eventIndex].MessageId;
                        properties.Headers = new Dictionary<string, Object>();

                        tracer.SetComponent(_compomentName);
                        tracer.SetTag("x-eventId", Events[eventIndex].EventId);
                        tracer.SetTag("x-messageId", Events[eventIndex].MessageId);
                        tracer.SetTag("x-traceId", Events[eventIndex].TraceId);                        
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
                            _channel.QueueDeclare(
                                        queue: newQueue,
                                        durable: true,
                                        exclusive: false,
                                        autoDelete: false,
                                        arguments: Events[eventIndex].Headers);


                            //发送至延时队列，延时结束后会写入正式度列
                            _batchPublish.Add(
                                    exchange: "",
                                    mandatory: true,
                                    routingKey: newQueue,
                                    properties: properties,
                                    body: bytes);
                        }
                        else
                        {
                            //发送到正常队列
                            _batchPublish.Add(
                                    exchange: _exchange,
                                    mandatory: true,
                                    routingKey: routeKey,
                                    properties: properties,
                                    body: bytes);
                        }
                    }
                }

                await _eventBusSenderRetryPolicy.Execute(async () =>
                {
                    await Task.Run(() =>
                    {
                        //批量提交
                        _batchPublish.Publish();

                    });
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw ex;
            }
        }

        async Task<bool> EnqueueConfirm(List<EventMessage> Events)
        {
            var persistentConnection = _senderLoadBlancer.Lease();

            try
            {
                if (!persistentConnection.IsConnected)
                {
                    persistentConnection.TryConnect();
                }
             
                var _channel = persistentConnection.GetModel();      

                // 提交走批量通道
                var _batchPublish = _channel.CreateBasicPublishBatch();

                for (var eventIndex = 0; eventIndex < Events.Count; eventIndex++)
                {
                    _eventBusSenderRetryPolicy.Execute(() =>
                    {
                        using (var tracer = new Hummingbird.Extensions.Tracing.Tracer("AMQP Publish"))
                        {
                            var json = Events[eventIndex].Body;
                            var routeKey = Events[eventIndex].RouteKey;
                            byte[] bytes = Encoding.UTF8.GetBytes(json);

                            //设置消息持久化
                            IBasicProperties properties = _channel.CreateBasicProperties();
                            properties.DeliveryMode = 2;
                            properties.MessageId = Events[eventIndex].MessageId;
                            properties.Headers = new Dictionary<string, Object>();     

                            tracer.SetComponent(_compomentName);
                            tracer.SetTag("x-messageId", Events[eventIndex].MessageId);
                            tracer.SetTag("x-eventId", Events[eventIndex].EventId);
                            tracer.SetTag("x-traceId", Events[eventIndex].TraceId);
                          
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
                                _channel.QueueDeclare(
                                                queue: newQueue,
                                                durable: true,
                                                exclusive: false,
                                                autoDelete: false,
                                                arguments: Events[eventIndex].Headers);

                                _batchPublish.Add(
                                        exchange: "",
                                        mandatory: true,
                                        routingKey: newQueue,
                                        properties: properties,
                                        body: bytes);
                            }
                            else
                            {
                                //发送到正常队列
                                _batchPublish.Add(
                                            exchange: _exchange,
                                            mandatory: true,
                                            routingKey: routeKey,
                                            properties: properties,
                                            body: bytes);
                            }
                        }

                    });
                }
           
                //批量提交
                _batchPublish.Publish();

                return _channel.WaitForConfirms(TimeSpan.FromMilliseconds(500));

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
        public IEventBus Register<TD, TH>(string QueueName, string EventTypeName = "")
                where TD : class
                where TH : IEventHandler<TD>
        {

            var persistentConnection = _receiveLoadBlancer.Lease();

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

                        var _channel = persistentConnection.CreateModel();
                        var _queueName = string.IsNullOrEmpty(QueueName) ? typeof(TH).FullName : QueueName;
                        var _routeKey = string.IsNullOrEmpty(EventTypeName) ? typeof(TD).FullName : EventTypeName;
                        var EventAction = _lifetimeScope.GetService(typeof(TH)) as IEventHandler<TD>;
                        if (EventAction == null)
                        {
                            EventAction = System.Activator.CreateInstance(typeof(TH)) as IEventHandler<TD>;
                        }

                        //direct fanout topic  
                        _channel.ExchangeDeclare(_exchange, _exchangeType, true, false, null);

                        //在MQ上定义一个持久化队列，如果名称相同不会重复创建
                        _channel.QueueDeclare(_queueName, true, false, false, null);
                        //绑定交换器和队列
                        _channel.QueueBind(_queueName, _exchange, _routeKey);
                        //绑定交换器和队列
                        _channel.QueueBind(_queueName, _exchange, _queueName);
                        //输入1，那如果接收一个消息，但是没有应答，则客户端不会收到下一个消息
                        _channel.BasicQos(0, _preFetch, false);
                        //在队列上定义一个消费者a
                        EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);

                        consumer.Received += async (ch, ea) =>
                        {
                            var EventId = -1L;
                            var MessageId = string.IsNullOrEmpty(ea.BasicProperties.MessageId) ? Guid.NewGuid().ToString("N") : ea.BasicProperties.MessageId;
                            var TraceId = MessageId;

                            if (ea.BasicProperties.Headers != null)
                            {
                                if (ea.BasicProperties.Headers.ContainsKey("x-eventId"))
                                {
                                    long.TryParse(System.Text.Encoding.UTF8.GetString(ea.BasicProperties.Headers["x-eventId"] as byte[]), out EventId);
                                }

                                if (ea.BasicProperties.Headers.ContainsKey("x-traceId"))
                                {
                                    TraceId = System.Text.Encoding.UTF8.GetString(ea.BasicProperties.Headers["x-traceId"] as byte[]);
                                }
                            }

                            using (var tracer = new Hummingbird.Extensions.Tracing.Tracer("AMQP Received",TraceId))
                            {
                                #region AMQP Received
                                try
                                {

                                    #region Ensure IsConnected
                                    if (!persistentConnection.IsConnected)
                                    {
                                        persistentConnection.TryConnect();
                                    }
                                    #endregion

                                    tracer.SetComponent(_compomentName);
                                    tracer.SetTag("queueName", _queueName);
                                    tracer.SetTag("x-messageId", MessageId);
                                    tracer.SetTag("x-eventId", EventId);
                                    tracer.SetTag("x-traceId", TraceId);

                                    var eventResponse = new EventResponse()
                                    {
                                        EventId = EventId,
                                        MessageId = MessageId,
                                        TraceId = TraceId,
                                        Headers = ea.BasicProperties.Headers ?? new Dictionary<string, object>(),
                                        Body = default(TD),
                                        QueueName = _queueName,
                                        RouteKey = _routeKey,                                       
                                        BodySource = Encoding.UTF8.GetString(ea.Body)
                                    };

                                    try
                                    {
                                        eventResponse.Body = JsonConvert.DeserializeObject<TD>(eventResponse.BodySource);
                                        _logger.LogInformation(eventResponse.BodySource);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, ex.Message);
                                    }
                                    
                                    if (!eventResponse.Headers.ContainsKey("x-exchange"))
                                    {
                                        eventResponse.Headers.Add("x-exchange", _exchange);
                                    }

                                    if (!eventResponse.Headers.ContainsKey("x-exchange-type"))
                                    {
                                        eventResponse.Headers.Add("x-exchange-type", _exchangeType);
                                    }

                                    #region AMQP ExecuteAsync
                                    using (var tracerExecuteAsync = new Hummingbird.Extensions.Tracing.Tracer("AMQP ExecuteAsync"))
                                    {
                                        try
                                        {
                                            var handlerOK = await _eventBusReceiverPolicy.ExecuteAsync(async (cancellationToken) =>
                                            {
                                                return await EventAction.Handle(eventResponse.Body, (Dictionary<string, object>)eventResponse.Headers, cancellationToken);

                                            }, CancellationToken.None);

                                            if (handlerOK)
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

                                                //重新入队，默认：是
                                                var requeue = true;

                                                try
                                                {
                                                    //执行回调，等待业务层确认是否重新入队
                                                    if (_subscribeNackHandler != null)
                                                    {
                                                        requeue = await _subscribeNackHandler((new EventResponse[] { eventResponse }, null));

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
                                        catch (Exception ex)
                                        {
                                            tracerExecuteAsync.SetError();

                                            //重新入队，默认：是
                                            var requeue = true;

                                            try
                                            {
                                                //执行回调，等待业务层的处理结果
                                                if (_subscribeNackHandler != null)
                                                {
                                                    requeue = await _subscribeNackHandler((new EventResponse[] { eventResponse }, ex));
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
                            _logger.LogDebug($"MQ:{_queueName} Consumer_Unregistered");
                        };

                        consumer.Registered += (ch, ea) =>
                        {
                            _logger.LogDebug($"MQ:{_queueName} Consumer_Registered");
                        };

                        consumer.Shutdown += (ch, ea) =>
                        {
                            _logger.LogDebug($"MQ:{_queueName} Consumer_Shutdown.{ea.ReplyText}");
                        };

                        consumer.ConsumerCancelled += (object sender, ConsumerEventArgs e) =>
                        {
                            _logger.LogDebug($"MQ:{_queueName} ConsumerCancelled");
                        };

                        //消费队列，并设置应答模式为程序主动应答
                        _channel.BasicConsume(_queueName, false, consumer);

                        _subscribeChannels.Add(_channel);
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
        public IEventBus RegisterBatch<TD, TH>(string QueueName, string EventTypeName = "", int BatchSize = 50)
                where TD : class
                where TH : IEventBatchHandler<TD>
        {
            var persistentConnection = _receiveLoadBlancer.Lease();

            if (!persistentConnection.IsConnected)
            {
                persistentConnection.TryConnect();
            }

            for (int parallelism = 0; parallelism < _reveiverMaxDegreeOfParallelism; parallelism++)
            {
                try
                {
                    var _channel = persistentConnection.CreateModel();
                    var _queueName = string.IsNullOrEmpty(QueueName) ? typeof(TH).FullName : QueueName;
                    var _routeKey = string.IsNullOrEmpty(EventTypeName) ? typeof(TD).FullName : EventTypeName;
                    var EventAction = _lifetimeScope.GetService(typeof(TH)) as IEventBatchHandler<TD>;

                    if (EventAction == null)
                    {
                        EventAction = System.Activator.CreateInstance(typeof(TH)) as IEventBatchHandler<TD>;
                    }

                    //direct fanout topic  
                    _channel.ExchangeDeclare(_exchange, _exchangeType, true, false, null);

                    //在MQ上定义一个持久化队列，如果名称相同不会重复创建
                    _channel.QueueDeclare(_queueName, true, false, false, null);
                    //绑定交换器和队列
                    _channel.QueueBind(_queueName, _exchange, _routeKey);
                    _channel.QueueBind(_queueName, _exchange, _queueName);
                    //输入1，那如果接收一个消息，但是没有应答，则客户端不会收到下一个消息
                    _channel.BasicQos(0, (ushort)BatchSize, false);

                    Task.Run(async () =>
                    {
                        while (true)
                        {
                            try
                            {
                                var batchPool = new List<(string MessageId, BasicGetResult ea)>();
                                var batchLastDeliveryTag = 0UL;

                                #region batch Pull
                                for (var i = 0; i < BatchSize; i++)
                                {
                                    var ea = _channel.BasicGet(_queueName, false);

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
                                    var basicGetResults = batchPool.Select(a => a.ea).ToArray();

                                    EventResponse[] Messages = new EventResponse[basicGetResults.Length];

                                    try
                                    {
                                        for (int i = 0; i < basicGetResults.Length; i++)
                                        {
                                            var ea = basicGetResults[i];

                                            Messages[i] = new EventResponse()
                                            {
                                                EventId = -1,
                                                MessageId = string.IsNullOrEmpty(ea.BasicProperties.MessageId) ? Guid.NewGuid().ToString("N") : ea.BasicProperties.MessageId,
                                                TraceId = string.Empty,
                                                Headers = ea.BasicProperties.Headers ?? new Dictionary<string, object>(),
                                                Body = default(TD),
                                                RouteKey = _routeKey,
                                                QueueName = _queueName,
                                                BodySource = Encoding.UTF8.GetString(ea.Body)
                                            };

                                            if(ea.BasicProperties.Headers!=null)
                                            {
                                                if (Messages[i].Headers.ContainsKey("x-eventId"))
                                                {
                                                    if (long.TryParse(System.Text.Encoding.UTF8.GetString(Messages[i].Headers["x-eventId"] as byte[]), out long EventId))
                                                    {
                                                        Messages[i].EventId = EventId;
                                                    }
                                                }

                                                if (!Messages[i].Headers.ContainsKey("x-traceId"))
                                                {
                                                    Messages[i].TraceId = System.Text.Encoding.UTF8.GetString(Messages[i].Headers["traceId"] as byte[]);
                                                }
                                                else
                                                {
                                                    Messages[i].TraceId = Messages[i].MessageId;
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

                                            using (var tracer = new Hummingbird.Extensions.Tracing.Tracer("AMQP BasicGet", Messages[i].TraceId))
                                            {
                                                tracer.SetComponent(_compomentName);
                                                tracer.SetTag("queueName", _queueName);
                                                tracer.SetTag("x-messageId", Messages[i].MessageId);
                                                tracer.SetTag("x-eventId", Messages[i].EventId);
                                                tracer.SetTag("x-traceId", Messages[i].TraceId);

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

                                        if (Messages != null && Messages.Any())
                                        {
                                            using (var tracer = new Hummingbird.Extensions.Tracing.Tracer("AMQP Execute"))
                                            {
                                                tracer.SetComponent(_compomentName);

                                                var handlerOK = await _eventBusReceiverPolicy.ExecuteAsync(async (cancellationToken) =>
                                                {
                                                    return await EventAction.Handle(Messages.Select(a => (TD)a.Body).ToArray(), Messages.Select(a => (Dictionary<string, object>)a.Headers).ToArray(), cancellationToken);

                                                }, CancellationToken.None);

                                                if (handlerOK)
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
                                                    tracer.SetError();

                                                    #region 消息处理失败
                                                    var requeue = true;
                                                    try
                                                    {
                                                        if (_subscribeNackHandler != null && Messages.Length > 0)
                                                        {
                                                            requeue = await _subscribeNackHandler((Messages, null));
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
                                    catch (Exception ex)
                                    {
                                        #region 业务处理消息出现异常，消息重新写入队列，超过最大重试次数后不再写入队列
                                        var requeue = true;

                                        try
                                        {
                                            if (_subscribeNackHandler != null && Messages.Length > 0)
                                            {
                                                requeue = await _subscribeNackHandler((Messages, ex));
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
                            catch (Exception ex)
                            {
                                _logger.LogError(ex.Message, ex);
                            }

                            System.Threading.Thread.Sleep(1);
                        }
                    });

                    _subscribeChannels.Add(_channel);
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
