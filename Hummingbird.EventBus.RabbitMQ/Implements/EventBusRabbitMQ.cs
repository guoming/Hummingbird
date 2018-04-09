using Autofac;
using Hummingbird.EventBus;
using Hummingbird.EventBus.Abstractions;
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

namespace Hummingbird.EventBus.RabbitMQ
{

    /// <summary>
    /// 消息队列
    /// 作者：郭明
    /// 日期：2017年4月5日
    /// </summary>
    public class EventBusRabbitMQ : IEventBus
    {
        private readonly ILifetimeScope _autofac;
        private readonly IRabbitMQPersistentConnection _persistentConnection;
        private readonly ILogger<EventBusRabbitMQ> _logger;
        private readonly string AUTOFAC_SCOPE_NAME = "event_bus";
        private readonly string _exchange = "amq.topic";
        private readonly string _exchangeType = "topic";
        private readonly ushort _preFetch = 1;
        private readonly int _retryCount = 3;

        private Action<string, string> _ackHandler = null;
        private Func<string, string, Exception, dynamic, Task<bool>> _nackHandler = null;
        private static List<IModel> subscribeChannels = new List<IModel>();


        public EventBusRabbitMQ(
            IRabbitMQPersistentConnection persistentConnection, 
            ILogger<EventBusRabbitMQ> logger,
            ILifetimeScope autofac,
            int retryCount=3,
            ushort preFetch =1,
            string exchange="amp.topic",
            string exchangeType = "topic")
        {
            this._autofac = autofac ?? throw new ArgumentNullException(nameof(autofac)); ; ;
            this._persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection)); ;
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._retryCount = retryCount;
            this._preFetch = preFetch;
            this._exchange = exchange;
            this._exchangeType = exchangeType;
        }

     
        /// <summary>
        /// 发送消息
        /// </summary>
        public void Publish(
            List<Models.EventLogEntry> Events,
            Action<List<string>> ackHandler = null,
            Action<List<string>> nackHandler = null,
            Action<List<string>> returnHandler = null,
            int EventDelaySeconds = 0,
            int TimeoutMilliseconds = 500)
        {
            var evtDicts = Events.Where(a => a.EventId != null).ToDictionary(a => a.EventId, msg => new Dictionary<string, string>()
            {
                { "Body",msg.Content},
                { "EventTypeName" ,msg.EventTypeName }
            });

            Enqueue(evtDicts, ackHandler, nackHandler, returnHandler, EventDelaySeconds, TimeoutMilliseconds);
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        void Enqueue(
            Dictionary<string,Dictionary<string,string>> Events,
            Action<List<string>> ackHandler = null,
            Action<List<string>> nackHandler = null,
            Action<List<string>> returnHandler = null,
            int EventDelaySeconds = 0,
            int TimeoutMilliseconds = 500)
        {
            try
            {
                if (!_persistentConnection.IsConnected)
                {
                    _persistentConnection.TryConnect();
                }

                

                var policy = RetryPolicy.Handle<BrokerUnreachableException>()
               .Or<SocketException>()
               .Or<System.IO.IOException>()
               .Or<AlreadyClosedException>()
               .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
               {
                   _logger.LogWarning(ex.ToString());
               });

                using (var _channel = _persistentConnection.CreateModel())
                {
                    var DeliveryTags = new Dictionary<ulong, string>();

                    //消息无法投递失被退回（如：队列找不到）
                    _channel.BasicReturn += (object sender, BasicReturnEventArgs e) =>
                    {
                        var EventIDs = new List<string>();

                        Console.WriteLine($"BasicReturn:MessageId={e.BasicProperties.MessageId},RoutingKey={e.RoutingKey},ReplyText={e.ReplyText}");
                        if (!string.IsNullOrEmpty(e.BasicProperties.MessageId))
                        {
                            EventIDs.Add(e.BasicProperties.MessageId);
                        }

                        if (returnHandler != null)
                        {
                            returnHandler(EventIDs);
                        }
                    };

                    //消息路由到队列并持久化后执行
                    _channel.BasicAcks += (object sender, BasicAckEventArgs e) =>
                    {
                        Console.WriteLine($"BasicAcks:DeliveryTag={e.DeliveryTag},Multiple={e.Multiple}");

                        var EventIDs = new List<string>();

                        if (e.Multiple)
                        {
                            foreach (var EventID in DeliveryTags.Where(a => a.Key < e.DeliveryTag + 1).Select(a => a.Value))
                            {
                                if (!EventIDs.Contains(EventID))
                                {
                                    EventIDs.Add(EventID);
                                }
                            }
                        }
                        else
                        {
                            var EventID = DeliveryTags[e.DeliveryTag];

                            if (!EventIDs.Contains(EventID))
                            {
                                EventIDs.Add(DeliveryTags[e.DeliveryTag]);
                            }
                        }

                        if (ackHandler != null)
                        {
                            ackHandler(EventIDs);
                        }

                    };

                    //消息投递失败
                    _channel.BasicNacks += (object sender, BasicNackEventArgs e) =>
                    {
                        Console.WriteLine($"BasicNacks:DeliveryTag={e.DeliveryTag},Multiple={e.Multiple},Requeue={e.Requeue}");

                        var EventIDs = new List<string>();

                        //批量确认
                        if (e.Multiple)
                        {
                            foreach (var EventID in DeliveryTags.Where(a => a.Key < e.DeliveryTag + 1).Select(a => a.Value))
                            {
                                if (!EventIDs.Contains(EventID))
                                {
                                    EventIDs.Add(EventID);
                                }
                            }
                        }
                        else
                        {
                            var EventID = DeliveryTags[e.DeliveryTag];

                            if (!EventIDs.Contains(EventID))
                            {
                                EventIDs.Add(EventID);
                            }
                        }

                        if (nackHandler != null)
                        {
                            nackHandler(EventIDs);
                        }
                    };

                    policy.Execute(() =>
                    {
                        _channel.ConfirmSelect();

                    });

                    foreach (var msg in Events)
                    {
                        
                      
                            policy.Execute(() =>
                            {
                                var EventId = msg.Key;
                                var json = msg.Value["Body"];
                                var routeKey = msg.Value["EventTypeName"];
                           
                                byte[] bytes = Encoding.UTF8.GetBytes(json);
                          

                                //设置消息持久化
                                IBasicProperties properties = _channel.CreateBasicProperties();
                                properties.DeliveryMode = 2;
                                properties.MessageId =msg.Key;

                                if (!DeliveryTags.ContainsValue(EventId))
                                {
                                    DeliveryTags.Add((ulong)DeliveryTags.Count + 1, EventId);
                                }

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

                                    _channel.BasicPublish(
                                        exchange: "",
                                        mandatory: true,
                                        routingKey: routeKey,
                                        basicProperties: properties,
                                        body: bytes);

                                }
                                else
                                {
                                    _channel.BasicPublish(
                                        exchange: _exchange,
                                        mandatory: true,
                                        routingKey: routeKey,
                                        basicProperties: properties,
                                        body: bytes);

                                }
                            });
                   
                    }

                    policy.Execute(() =>
                    {
                        _channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(TimeoutMilliseconds));
                    });
                }
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
        public IEventBus Register<TD, TH>(string EventTypeName="")
                where TD : EventEntity
                where TH : IEventHandler<TD>
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            var _channel = _persistentConnection.CreateModel();

            var _queueName = typeof(TH).FullName;
            var _routeKey = string.IsNullOrEmpty(EventTypeName) ? typeof(TD).FullName: EventTypeName;
            var EventAction = _autofac.ResolveOptional(typeof(TH)) as IEventHandler<TD>;

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

                try
                {

                    bytes = ea.Body;
                    str = Encoding.UTF8.GetString(bytes);
                    msg = JsonConvert.DeserializeObject<TD>(str);
                    var eventActionTask = EventAction.Handle(msg);
                    Task.WaitAll(eventActionTask);

                    if (eventActionTask.Result)
                    {
                        if (_ackHandler != null)
                        {
                            _ackHandler(EventId, _queueName);
                        }

                        //回复确认
                        _channel.BasicAck(ea.DeliveryTag, false);

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
