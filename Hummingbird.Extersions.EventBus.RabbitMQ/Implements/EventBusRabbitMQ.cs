using Autofac;
using Hummingbird.Extersions.Cache;
using Hummingbird.Extersions.EventBus;
using Hummingbird.Extersions.EventBus.Abstractions;
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
using System.Threading.Tasks.Dataflow;

namespace Hummingbird.Extersions.EventBus.RabbitMQ
{
    public struct EventMessage
    {
        public string MessageId { get; set; }
        public string Body { get; set; }

        public string RouteKey { get; set; }
    }


    /// <summary>
    /// 消息队列
    /// 作者：郭明
    /// 日期：2017年4月5日
    /// </summary>
    public class EventBusRabbitMQ : IEventBus
    {
        private readonly IServiceProvider _lifetimeScope;
        private readonly IRabbitMQPersistentConnection _persistentConnection;
        private readonly ILogger<EventBusRabbitMQ> _logger;
        private readonly string AUTOFAC_SCOPE_NAME = "event_bus";
        private readonly string _exchange = "amq.topic";
        private readonly string _exchangeType = "topic";
        private readonly ushort _preFetch = 1;
        private readonly int _retryCount = 3;
        private readonly int _IdempotencyDuration;

        private Action<string[], string> _subscribeAckHandler = null;
        private Func<string[], string, Exception, dynamic[], Task<bool>> _subscribeNackHandler = null;
        private static List<IModel> _subscribeChannels = new List<IModel>();
        private readonly IHummingbirdCache<bool> _cacheManager;
        private readonly RetryPolicy _eventBusRetryPolicy = null;

        public EventBusRabbitMQ(
            IHummingbirdCache<bool> cacheManager,
            IRabbitMQPersistentConnection persistentConnection,
            ILogger<EventBusRabbitMQ> logger,
           IServiceProvider lifetimeScope,
            int retryCount = 3,
            ushort preFetch = 1,
            int IdempotencyDuration = 15,
            string exchange = "amp.topic",
            string exchangeType = "topic")
        {
            this._lifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));
            this._IdempotencyDuration = IdempotencyDuration;
            this._cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
            this._persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection)); ;
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._retryCount = retryCount;
            this._preFetch = preFetch;
            this._exchange = exchange;
            this._exchangeType = exchangeType;
            this._eventBusRetryPolicy = RetryPolicy.Handle<BrokerUnreachableException>()
           .Or<SocketException>()
           .Or<System.IO.IOException>()
           .Or<AlreadyClosedException>()
           .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
           {
               _logger.LogWarning(ex.ToString());
           });
        }


        /// <summary>
        /// 发送消息
        /// </summary>
        public async Task PublishAsync(
            List<Models.EventLogEntry> Events,
            Action<List<string>> ackHandler = null,
            Action<List<string>> nackHandler = null,
            Action<List<string>> returnHandler = null,
            int EventDelaySeconds = 0,
            int TimeoutMilliseconds = 500,
            int BatchSize = 500)
        {

            var evtDicts = Events.Where(a => a.EventId != null).Select(a => new EventMessage()
            {
                Body = a.Content,
                MessageId = a.EventId,
                RouteKey = a.EventTypeName

            }).ToList();

            await Enqueue(evtDicts, ackHandler, nackHandler, returnHandler, EventDelaySeconds, TimeoutMilliseconds, BatchSize);
        }

        async Task Enqueue(
          List<EventMessage> Events,
          Action<List<string>> ackHandler,
          Action<List<string>> nackHandler,
          Action<List<string>> returnHandler,
          int EventDelaySeconds,
          int TimeoutMilliseconds,
          int BatchSize)
        {
            try
            {
                if (!_persistentConnection.IsConnected)
                {
                    _persistentConnection.TryConnect();
                }
                //消息发送成功后回调后需要修改数据库状态，改成本地做组缓存后，再批量入库。（性能提升百倍）
                var _batchBlock_BasicReturn = new BatchBlock<string>(BatchSize);
                var _batchBlock_BasicAcks = new BatchBlock<string>(BatchSize);
                var _batchBlock_BasicNacks = new BatchBlock<string>(BatchSize);
                var _actionBlock_BasicReturn = new ActionBlock<string[]>(EventIDs =>
                {
                    if (returnHandler != null && EventIDs.Length > 0)
                    {
                        returnHandler(EventIDs.ToList());
                    }
                }, new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                });

                var _actionBlock_BasicAcks = new ActionBlock<string[]>(EventIDs =>
                {
                    if (ackHandler != null && EventIDs.Length > 0)
                    {
                        ackHandler(EventIDs.ToList());
                    }
                }, new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                });

                var _actionBlock_BasicNacks = new ActionBlock<string[]>(EventIDs =>
                {
                    if (nackHandler != null && EventIDs.Length > 0)
                    {
                        nackHandler(EventIDs.ToList());
                    }
                }, new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                });

                _batchBlock_BasicReturn.LinkTo(_actionBlock_BasicReturn);
                _batchBlock_BasicAcks.LinkTo(_actionBlock_BasicAcks);
                _batchBlock_BasicNacks.LinkTo(_actionBlock_BasicNacks);

                using (var _channel = _persistentConnection.CreateModel())
                {
                    //保存EventId和DeliveryTag 映射
                    var unconfirmEventIds = new string[Events.Count];
                    var returnEventIds = new Dictionary<string, bool>();
                    ulong lastDeliveryTag = 0;

                    //消息无法投递失被退回（如：队列找不到）
                    _channel.BasicReturn += async (object sender, BasicReturnEventArgs e) =>
                    {

                        if (!string.IsNullOrEmpty(e.BasicProperties.MessageId))
                        {
                            returnEventIds.Add(e.BasicProperties.MessageId, false);
                            await _batchBlock_BasicReturn.SendAsync(e.BasicProperties.MessageId);
                        }
                    };

                    //消息路由到队列并持久化后执行
                    _channel.BasicAcks += async (object sender, BasicAckEventArgs e) =>
                    {
                        if (e.Multiple)
                        {
                            for (var i = lastDeliveryTag; i < e.DeliveryTag; i++)
                            {
                                var eventId = unconfirmEventIds[i];
                                if (!string.IsNullOrEmpty(eventId))
                                {
                                    unconfirmEventIds[i] = "";

                                    if (returnEventIds.Count > 0)
                                    {
                                        if (!returnEventIds.ContainsKey(eventId))
                                        {
                                            await _batchBlock_BasicAcks.SendAsync(eventId);
                                        }
                                    }
                                    else
                                    {
                                        await _batchBlock_BasicAcks.SendAsync(eventId);
                                    }
                                }
                            }

                            // 批量回调，记录当期位置
                            lastDeliveryTag = e.DeliveryTag;
                        }
                        else
                        {
                            var eventId = unconfirmEventIds[e.DeliveryTag - 1];

                            if (!string.IsNullOrEmpty(eventId))
                            {
                                unconfirmEventIds[e.DeliveryTag - 1] = "";
                                if (returnEventIds.Count > 0)
                                {
                                    if (!returnEventIds.ContainsKey(eventId))
                                    {
                                        await _batchBlock_BasicAcks.SendAsync(eventId);
                                    }
                                }
                                else
                                {
                                    await _batchBlock_BasicAcks.SendAsync(eventId);
                                }
                            }
                        }
                    };

                    //消息投递失败
                    _channel.BasicNacks += async (object sender, BasicNackEventArgs e) =>
                    {

                        if (e.Multiple)
                        {
                            for (var i = lastDeliveryTag; i < e.DeliveryTag; i++)
                            {
                                var eventId = unconfirmEventIds[i];
                                if (!string.IsNullOrEmpty(eventId))
                                {
                                    unconfirmEventIds[i] = "";

                                    if (returnEventIds.Count > 0)
                                    {
                                        if (!returnEventIds.ContainsKey(eventId))
                                        {
                                            await _batchBlock_BasicNacks.SendAsync(eventId);
                                        }
                                    }
                                    else
                                    {
                                        await _batchBlock_BasicNacks.SendAsync(eventId);
                                    }
                                }
                            }

                            // 批量回调，记录当期位置
                            lastDeliveryTag = e.DeliveryTag;
                        }
                        else
                        {
                            var eventId = unconfirmEventIds[e.DeliveryTag - 1];
                            if (string.IsNullOrEmpty(eventId))
                            {
                                unconfirmEventIds[e.DeliveryTag - 1] = "";

                                if (returnEventIds.Count > 0)
                                {
                                    if (!returnEventIds.ContainsKey(eventId))
                                    {
                                        await _batchBlock_BasicNacks.SendAsync(eventId);
                                    }
                                }
                                else
                                {
                                    await _batchBlock_BasicNacks.SendAsync(eventId);
                                }
                            }

                        }
                    };

                    _eventBusRetryPolicy.Execute(() =>
                    {
                        _channel.ConfirmSelect();
                    });

                    // 提交走批量通道
                    var _batchPublish = _channel.CreateBasicPublishBatch();


                    for (var eventIndex = 0; eventIndex < Events.Count; eventIndex++)
                    {
                        _eventBusRetryPolicy.Execute(() =>
                        {
                            var MessageId = Events[eventIndex].MessageId;
                            var json = Events[eventIndex].Body;
                            var routeKey = Events[eventIndex].RouteKey;
                            byte[] bytes = Encoding.UTF8.GetBytes(json);
                            //设置消息持久化
                            IBasicProperties properties = _channel.CreateBasicProperties();
                            properties.DeliveryMode = 2;
                            properties.MessageId = MessageId;

                            unconfirmEventIds[eventIndex] = MessageId;

                            //需要发送延时消息
                            if (EventDelaySeconds > 0)
                            {
                                Dictionary<string, object> dic = new Dictionary<string, object>();
                                dic.Add("x-expires", EventDelaySeconds * 10000);//队列过期时间 
                                dic.Add("x-message-ttl", EventDelaySeconds * 1000);//当一个消息被推送在该队列的时候 可以存在的时间 单位为ms，应小于队列过期时间  
                                dic.Add("x-dead-letter-exchange", _exchange);//过期消息转向路由  
                                dic.Add("x-dead-letter-routing-key", routeKey);//过期消息转向路由相匹配routingkey  
                                routeKey = routeKey + "_DELAY_" + EventDelaySeconds;

                                //创建一个队列                         
                                _channel.QueueDeclare(
                                       queue: routeKey,
                                       durable: true,
                                       exclusive: false,
                                       autoDelete: false,
                                       arguments: dic);

                                _batchPublish.Add(
                                    exchange: "",
                                    mandatory: true,
                                    routingKey: routeKey,
                                    properties: properties,
                                    body: bytes);

                            }
                            else
                            {
                                _batchPublish.Add(
                                    exchange: _exchange,
                                    mandatory: true,
                                    routingKey: routeKey,
                                    properties: properties,
                                    body: bytes);
                            }
                        });
                    };

                    await _eventBusRetryPolicy.Execute(async () =>
                    {
                        await Task.Run(() =>
                        {
                            //批量提交
                            _batchPublish.Publish();
                            _channel.WaitForConfirms(TimeSpan.FromMilliseconds(TimeoutMilliseconds));
                        });
                    });
                }

                _batchBlock_BasicAcks.Complete();
                _batchBlock_BasicNacks.Complete();
                _batchBlock_BasicReturn.Complete();

                await _batchBlock_BasicReturn.Completion.ContinueWith(delegate { _actionBlock_BasicReturn.Complete(); });
                await _batchBlock_BasicAcks.Completion.ContinueWith(delegate { _actionBlock_BasicAcks.Complete(); });
                await _batchBlock_BasicNacks.Completion.ContinueWith(delegate { _actionBlock_BasicNacks.Complete(); });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

            }
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
        public IEventBus Register<TD, TH>(string EventTypeName = "")
                where TD : class
                where TH : IEventHandler<TD>
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }
           
            var _channel = _persistentConnection.CreateModel();
            var policy = createPolicy();
            var msgHandlerPolicy = Policy<Boolean>.Handle<Exception>().FallbackAsync(false)
                .WrapAsync(policy);

            var _queueName = typeof(TH).FullName;
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
            //输入1，那如果接收一个消息，但是没有应答，则客户端不会收到下一个消息
            _channel.BasicQos(0, _preFetch, false);
            //在队列上定义一个消费者a
            EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (ch, ea) =>
            {
                if (!_persistentConnection.IsConnected)
                {
                    _persistentConnection.TryConnect();
                }
                byte[] bytes;
                string str = string.Empty;
                var msg = default(TD);
                var EventId = ea.BasicProperties.MessageId;
                if (string.IsNullOrEmpty(EventId) || _IdempotencyDuration == 0 || !_cacheManager.Exists(EventId, "Events"))
                {
                    try
                    {
                        bytes = ea.Body;
                        str = Encoding.UTF8.GetString(bytes);
                        msg = JsonConvert.DeserializeObject<TD>(str);

                        var handlerOK = await msgHandlerPolicy.ExecuteAsync(async (cancellationToken) =>
                        {
                            return await EventAction.Handle(msg,cancellationToken);

                        },CancellationToken.None);

                        if (handlerOK)
                        {
                            if (_subscribeAckHandler != null)
                            {
                                _subscribeAckHandler(new string[] { EventId }, _queueName);
                            }

                            //回复确认
                            _channel.BasicAck(ea.DeliveryTag, false);

                            //消费端保证幂等时需要积
                            if (_IdempotencyDuration > 0 && !string.IsNullOrEmpty(EventId))
                            {
                                _cacheManager.Add(EventId, true, TimeSpan.FromSeconds(_IdempotencyDuration), "Events");
                            }
                        }
                        else
                        {
                            var requeue = true;

                            if (_subscribeNackHandler != null)
                            {
                                requeue = await _subscribeNackHandler(new string[] { EventId }, _queueName, null, new dynamic[] { msg });

                            }

                            //拒绝重新写入队列，处理
                            _channel.BasicReject(ea.DeliveryTag, requeue);

                        }
                    }
                    catch (Exception ex)
                    {
                        var requeue = true;

                        if (_subscribeNackHandler != null)
                        {
                            requeue = await _subscribeNackHandler(new string[] { EventId }, _queueName, ex, new dynamic[] { msg });

                        }
                        _channel.BasicReject(ea.DeliveryTag, requeue);
                    }
                }
                else
                {
                    //回复确认
                    _channel.BasicAck(ea.DeliveryTag, false);
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
            return this;
        }

        private IAsyncPolicy createPolicy() {

            IAsyncPolicy policy = Policy.NoOpAsync();//创建一个空的Policy

            //设置熔断策略
            policy = policy.WrapAsync(Policy.Handle<Exception>()
                .AdvancedCircuitBreakerAsync(
                    failureThreshold: 0.5, // Break on >=50% actions result in handled exceptions...
                    samplingDuration: TimeSpan.FromSeconds(10), // ... over any 10 second period
                    minimumThroughput: 8, // ... provided at least 8 actions in the 10 second period.
                    durationOfBreak: TimeSpan.FromSeconds(30), // Break for 30 seconds.
                    onBreak: (Exception exception, TimeSpan timeSpan) =>
                    {
                        Console.WriteLine("onBreak!");
                    },
                    onHalfOpen: () =>
                    {
                        Console.WriteLine("onReset!");
                    },
                    onReset: () =>
                    {
                        Console.WriteLine("onReset!");
                    }));

            //设置重试策略
            policy = policy.WrapAsync(Policy.Handle<Exception>()
                   .RetryAsync(3, (ex, time) =>
                   {
                       _logger.LogError(ex, ex.ToString());
                   }));


            // 设置超时
            policy = policy.WrapAsync(Policy.TimeoutAsync(
                TimeSpan.FromSeconds(2),
                TimeoutStrategy.Pessimistic,
                (context, timespan, task) =>
                {
                    Console.WriteLine("Timeout!");

                    return Task.FromResult(true);
                }));

     

            return policy;

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
        public IEventBus RegisterBatch<TD, TH>(string EventTypeName = "", int BatchSize =50)
                where TD : class
                where TH : IEventBatchHandler<TD>
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            var _channel = _persistentConnection.CreateModel();
            var policy = createPolicy();
            var msgHandlerPolicy = Policy<Boolean>.Handle<Exception>().FallbackAsync(false)
                .WrapAsync(policy);
            var _queueName = typeof(TH).FullName;
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
            //输入1，那如果接收一个消息，但是没有应答，则客户端不会收到下一个消息
            _channel.BasicQos(0,(ushort)BatchSize, false);

            Task.Run(async () =>
            {
                while (true)
                {
                    var batchPool = new ConcurrentDictionary<string, TD>();
                    ulong batchLastDeliveryTag = 0;     
                    var _insertPoolBlock = new ActionBlock<BasicGetResult>(ea =>
                    {
                        if (string.IsNullOrEmpty(ea.BasicProperties.MessageId) || _IdempotencyDuration == 0 || !_cacheManager.Exists(ea.BasicProperties.MessageId, "Events"))
                        {
                            batchPool.TryAdd(ea.BasicProperties.MessageId, JsonConvert.DeserializeObject<TD>(Encoding.UTF8.GetString(ea.Body)));
                            batchLastDeliveryTag = ea.DeliveryTag;
                        }
                        else
                        {
                            _channel.BasicNack(ea.DeliveryTag, false, false);
                        }
                    });

                    #region batch Pull
                    for (var i = 0; i < BatchSize; i++)
                    {
                        var ea = _channel.BasicGet(_queueName, false);
                        if (ea != null)
                        {
                            _insertPoolBlock.Post(ea);
                        }
                        else
                        {
                            break;
                        }
                    }
                   
                    #endregion

                    _insertPoolBlock.Complete();
                    await _insertPoolBlock.Completion;

                    if (batchPool.Count > 0)
                    {
                        var eventIds = batchPool.Select(a => a.Key).ToArray();
                        var bodys = batchPool.Select(a => a.Value).ToArray();

                        try
                        {
                            var handlerOK= await msgHandlerPolicy.ExecuteAsync(async (cancellationToken) =>
                            {
                                return await EventAction.Handle(bodys, cancellationToken);

                            }, CancellationToken.None);

                            if (handlerOK)
                            {
                                #region 消息处理成功
                                if (_subscribeAckHandler != null)
                                {
                                    _subscribeAckHandler(eventIds, _queueName);
                                }
                                //回复确认
                                _channel.BasicAck(batchLastDeliveryTag, true);

                                //消息幂等
                                if (_IdempotencyDuration > 0)
                                {
                                    for (int i = 0; i < eventIds.Length; i++)
                                    {
                                        _cacheManager.Add(eventIds[i], true, TimeSpan.FromSeconds(_IdempotencyDuration), "Events");
                                    }
                                }

                                #endregion
                            }
                            else
                            {
                                #region 消息处理失败
                                var requeue = true;

                                if (_subscribeNackHandler != null)
                                {
                                    requeue = await _subscribeNackHandler(eventIds, _queueName, null, bodys);
                                }

                                _channel.BasicNack(batchLastDeliveryTag, true, requeue);

                                #endregion
                            }
                        }
                        catch (Exception ex)
                        {
                            #region 业务处理消息出现异常，消息重新写入队列，超过最大重试次数后不再写入队列
                            var requeue = true;

                            if (_subscribeNackHandler != null)
                            {
                                requeue = await _subscribeNackHandler(eventIds, _queueName, ex, bodys);
                            }
                            _channel.BasicNack(batchLastDeliveryTag, true, requeue);

                            #endregion
                        }
                    }
                }
            });
            _subscribeChannels.Add(_channel);
            return this;
        }

        public IEventBus Subscribe(
            Action<string[], string> ackHandler,
            Func<string[], string, Exception, dynamic[], Task<bool>> nackHandler)
        {
            _subscribeAckHandler = ackHandler;
            _subscribeNackHandler = nackHandler;
            return this;
        }
    }
}
