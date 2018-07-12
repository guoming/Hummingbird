using Autofac;
using Hummingbird.Extersions.Cache;
using Hummingbird.Extersions.EventBus;
using Hummingbird.Extersions.EventBus.Abstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
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

        private Action<string, string> _ackHandler = null;
        private Func<string, string, Exception, dynamic, Task<bool>> _nackHandler = null;
        private static List<IModel> subscribeChannels = new List<IModel>();
        private readonly IHummingbirdCache<bool> _cacheManager;
        private readonly RetryPolicy _policy = null;

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
            this._policy = RetryPolicy.Handle<BrokerUnreachableException>()
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
            int BatchSize=500)
        {

            var evtDicts = Events.Where(a => a.EventId != null).Select(a => new EventMessage()
            {
                 Body= a.Content,
                  MessageId= a.EventId,
                   RouteKey=a.EventTypeName

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
                    var deliveryTags = new List<string>();
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
                                var eventId = deliveryTags[(int)i];

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

                            // 批量回调，记录当期位置
                            lastDeliveryTag = e.DeliveryTag;
                        }
                        else
                        {
                            var eventId = deliveryTags[(int)e.DeliveryTag - 1];

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
                    };

                    //消息投递失败
                    _channel.BasicNacks += async (object sender, BasicNackEventArgs e) =>
                    {

                        if (e.Multiple)
                        {
                            for (var i = lastDeliveryTag; i < e.DeliveryTag; i++)
                            {
                                var eventId = deliveryTags[(int)i];

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

                            // 批量回调，记录当期位置
                            lastDeliveryTag = e.DeliveryTag;
                        }
                        else
                        {
                            var eventId = deliveryTags[(int)e.DeliveryTag - 1];
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
                    };

                    _policy.Execute(() =>
                    {
                        _channel.ConfirmSelect();
                    });

                    // 提交走批量通道
                    var _batchPublish = _channel.CreateBasicPublishBatch();
                    

                    for (var eventIndex = 0; eventIndex < Events.Count; eventIndex++)
                    {
                        _policy.Execute(() =>
                        {
                            var MessageId = Events[eventIndex].MessageId;
                            var json = Events[eventIndex].Body;
                            var routeKey = Events[eventIndex].RouteKey;
                            byte[] bytes = Encoding.UTF8.GetBytes(json);
                            //设置消息持久化
                            IBasicProperties properties = _channel.CreateBasicProperties();
                            properties.DeliveryMode = 2;
                            properties.MessageId = MessageId;

                            deliveryTags.Add(MessageId);

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

                    _policy.Execute(() =>
                    {
                        //批量提交
                        _batchPublish.Publish();
                        _channel.WaitForConfirms(TimeSpan.FromMilliseconds(TimeoutMilliseconds));
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
                        var handlerOK = await EventAction.Handle(msg);

                        if (handlerOK)
                        {
                            if (_ackHandler != null)
                            {
                                _ackHandler(EventId, _queueName);
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

                            if (_nackHandler != null)
                            {
                                requeue = await _nackHandler(EventId, _queueName, null, msg);
                            }

                            //拒绝重新写入队列，处理
                            _channel.BasicReject(ea.DeliveryTag, requeue);

                        }
                    }
                    catch (Exception ex)
                    {
                        var requeue = true;

                        if (_nackHandler != null)
                        {
                            requeue = await _nackHandler(EventId, _queueName, ex, msg);
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
        
            subscribeChannels.Add(_channel);
            return this;
        }

        public IEventBus Subscribe(
            Action<string, string> ackHandler,
            Func<string, string, Exception, dynamic, Task<bool>> nackHandler)
        {
            _ackHandler = ackHandler;
            _nackHandler = nackHandler;
            return this;
        }
    }
}
