
using Hummingbird.Extersions.EventBus;
using Hummingbird.Extersions.EventBus.Abstractions;
using Hummingbird.Extersions.EventBus.Models;
using Hummingbird.Extersions.EventBus.RabbitMQ;
using Hummingbird.Extersions.EventBus.RabbitMQ.LoadBalancers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    public class RabbitMqOption
    {

        /// <summary>
        /// 终结点设置
        /// </summary>
        /// <param name="HostName">地址</param>
        /// <param name="Port">端口</param>
        public void WithEndPoint(string HostName= "locahost", int Port=5672)
        {
            this.HostName = HostName;
            this.Port = Port;
        }


        /// <summary>
        /// 设置认证信息
        /// </summary>
        /// <param name="UserName">账号</param>
        /// <param name="Password">密码</param>
        public void WithAuth(string UserName,string Password)
        {
            this.UserName = UserName;
            this.Password = Password;
        }


        /// <summary>
        /// 设置交换器信息
        /// </summary>
        /// <param name="VirtualHost">虚拟机</param>
        /// <param name="ExchangeType">交换器类型</param>
        /// <param name="Exchange">交换器名称</param>
        public void WithExchange(string VirtualHost="/",string ExchangeType= "topic", string Exchange= "amq.topic")
        {
            this.VirtualHost = VirtualHost;
            this.ExchangeType = ExchangeType;
            this.Exchange = Exchange;
        }

        /// <summary>
        /// 设置连接池信息
        /// </summary>
        /// <param name="SenderMaxConnections">发送端最大连接数量</param>
        /// <param name="ReceiverMaxConnections">消费端最大连接数量</param>
        /// <param name="AcquireRetryAttempts">最大重试次数</param>
        public void WithSender(int SenderMaxConnections=10,int AcquireRetryAttempts=3,string LoadBalancer= "RoundRobinLoadBalancer")
        {
            this.SenderMaxConnections = SenderMaxConnections;
            this.SenderAcquireRetryAttempts = AcquireRetryAttempts;
            this.SenderLoadBalancer = LoadBalancer;
        }


        /// <summary>
        /// 消费端设置
        /// </summary>
        /// <param name="ReceiverMaxConnections">消费最大连接数</param>
        /// <param name="ReceiverAcquireRetryAttempts">最大重试次数</param>
        /// <param name="IdempotencyDurationSeconds">幂等持续时间（秒）</param>
        /// <param name="PreFetch">预取数量</param>
        public void WithReceiver(
            int ReceiverMaxConnections = 2, 
            int ReveiverMaxDegreeOfParallelism = 10,
            int ReceiverAcquireRetryAttempts = 0, 
            int ReceiverHandlerTimeoutMillseconds=0,
            string LoadBalancer= "RoundRobinLoadBalancer", int IdempotencyDurationSeconds=15, ushort PreFetch=1)
        {
            this.ReceiverMaxConnections = ReceiverMaxConnections;
            this.ReveiverMaxDegreeOfParallelism = ReveiverMaxDegreeOfParallelism;
            this.ReceiverAcquireRetryAttempts = ReceiverAcquireRetryAttempts;
            this.ReceiverHandlerTimeoutMillseconds = ReceiverHandlerTimeoutMillseconds;
            this.IdempotencyDuration = IdempotencyDurationSeconds;
            this.PreFetch = PreFetch;
            this.ReceiverLoadBalancer = LoadBalancer;
            
        }

        #region Endpoint
        /// <summary>
        /// 服务器地址(默认:localhost)
        /// </summary>
        internal string HostName { get; set; } = "locahost";
        /// <summary>
        /// 端口（默认：5672）
        /// </summary>
        internal int Port { get; set; } = 5672;
        #endregion

        #region Auth

        /// <summary>
        /// 账号(默认:guest)
        /// </summary>
        internal string UserName { get; set; } = "guest";

        /// <summary>
        /// 密码(默认:guest)
        /// </summary>
        internal string Password { get; set; } = "guest";
        #endregion

        #region Exchange
        /// <summary>
        /// 虚拟主机(默认：/)
        /// </summary>
        internal string VirtualHost { get; set; } = "/";


        /// <summary>
        /// 交换机名称(默认：amq.topic)
        /// </summary>
        internal string Exchange { get; set; } = "amq.topic";

        /// <summary>
        /// 交换机类型（默认：topic）
        /// </summary>
        internal string ExchangeType { get; set; } = "topic";
        #endregion


        #region Sender
        /// <summary>
        /// 重试次数(默认：3)
        /// </summary>
        internal int SenderAcquireRetryAttempts { get; set; } = 3;

        internal int SenderMaxConnections { get; set; } = 10;

        internal string SenderLoadBalancer { get; set; }

        #endregion


        #region Receiver

        /// <summary>
        /// 消费者连接数量
        /// </summary>
        internal int ReceiverMaxConnections { get; set; } = 2;
        /// <summary>
        /// 消费者负载均衡器
        /// </summary>
        internal string ReceiverLoadBalancer { get; set; }

        /// <summary>
        /// 消费者最大重试次数
        /// </summary>
        internal int ReceiverAcquireRetryAttempts { get; set; } = 3;

        /// <summary>
        /// 消费处理超时时间
        /// </summary>
        internal int ReceiverHandlerTimeoutMillseconds { get; set; } = 1000 * 2;
   
        /// <summary>
        /// 消费单个连接最大Channel数量
        /// </summary>
        internal int ReveiverMaxDegreeOfParallelism { get; set; } = 10;

        /// <summary>
        /// 默认获取
        /// </summary>
        internal ushort PreFetch { get; set; } = 1;

        /// <summary>
        /// 幂等持续时间（默认:15秒）
        /// </summary>
        internal int IdempotencyDuration { get; set; } = 15;
        #endregion

    }

    public static class DependencyInjectionExtersion
    {
        public static IHummingbirdEventBusHostBuilder AddRabbitmq(this IHummingbirdEventBusHostBuilder hostBuilder, Action<RabbitMqOption>  setupConnectionFactory)
        {
            setupConnectionFactory = setupConnectionFactory ?? throw new ArgumentNullException(nameof(setupConnectionFactory));

            var option = new RabbitMqOption();
            setupConnectionFactory(option);

            hostBuilder.Services.AddSingleton<IConnectionFactory>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IConnectionFactory>>();

                var factory = new ConnectionFactory();
                factory.HostName = option.HostName;
                factory.Port = option.Port;
                factory.Password = option.Password;
                factory.UserName = option.UserName;
                factory.VirtualHost = option.VirtualHost;
                factory.AutomaticRecoveryEnabled = true;
                factory.TopologyRecoveryEnabled = true;
                factory.UseBackgroundThreadsForIO = true;
                return factory;
            });
            hostBuilder.Services.AddSingleton<IRabbitMQPersisterConnectionLoadBalancerFactory>(sp =>
            {
                return new DefaultLoadBalancerFactory();
            });
            hostBuilder.Services.AddSingleton<IEventBus, EventBusRabbitMQ>(sp =>
            {
                var cache = sp.GetRequiredService<Hummingbird.Extersions.Cacheing.ICacheManager>();                
                var logger = sp.GetRequiredService<ILogger<IEventBus>>();
                var loggerConnection = sp.GetRequiredService<ILogger<IRabbitMQPersistentConnection>>();
                var rabbitMQPersisterConnectionLoadBalancerFactory = sp.GetRequiredService<IRabbitMQPersisterConnectionLoadBalancerFactory>();
                var connectionFactory = sp.GetRequiredService<IConnectionFactory>();                
                var senderConnections = new List<IRabbitMQPersistentConnection>();
                var receiveConnections = new List<IRabbitMQPersistentConnection>();

                //消费端连接池
                for (int i = 0; i < option.ReceiverMaxConnections; i++)
                {
                    //消费端的连接池
                    receiveConnections.Add(new DefaultRabbitMQPersistentConnection(connectionFactory, loggerConnection, option.ReceiverAcquireRetryAttempts));
                }

                //发送端连接池
                for (int i = 0; i < option.SenderMaxConnections; i++)
                {
                    senderConnections.Add(new DefaultRabbitMQPersistentConnection(connectionFactory, loggerConnection, option.SenderAcquireRetryAttempts));
                }

                var receiveLoadBlancer = rabbitMQPersisterConnectionLoadBalancerFactory.Get(()=> receiveConnections, option.ReceiverLoadBalancer);
                var senderLoadBlancer = rabbitMQPersisterConnectionLoadBalancerFactory.Get(()=> senderConnections, option.SenderLoadBalancer);
                
                return new EventBusRabbitMQ(cache,
                    receiveLoadBlancer,
                    senderLoadBlancer,
                    logger,
                    sp,
                    senderRetryCount: option.SenderAcquireRetryAttempts,
                    reveiverMaxDegreeOfParallelism:option.ReveiverMaxDegreeOfParallelism,
                    receiverAcquireRetryAttempts: option.ReceiverAcquireRetryAttempts,
                    receiverHandlerTimeoutMillseconds: option.ReceiverHandlerTimeoutMillseconds,
                    preFetch:option.PreFetch,
                    exchange: option.Exchange,
                    exchangeType: option.ExchangeType
                    );
            });

            return hostBuilder;
           
        }
    }

    public static class EventLogEntryExtersions
    {
        /// <summary>
        /// 设置重试策略
        /// </summary>
        /// <param name="event"></param>
        /// <param name="MaxRetries">最大重试次数</param>
        /// <param name="NumberOfRetries">当前重试次数</param>
        /// <returns></returns>
        public static void WithRetry(this EventLogEntry @event, int MaxRetries, int NumberOfRetries = 0)
        {
            @event.Headers.Add("x-message-max-retries", MaxRetries);
            @event.Headers.Add("x-message-retries", NumberOfRetries);
        }

        /// <summary>
        /// 设置延时策略
        /// </summary>
        /// <param name="event"></param>
        /// <param name="TTL">延时时间（秒）</param>
        /// <returns></returns>
        public static void WithWaitSeconds(this EventLogEntry @event, int TTL)
        {
            @event.Headers.Add("x-first-death-queue", $"{@event.EventTypeName}@Delay#{TTL}"); //死信队列名称
            @event.Headers.Add("x-message-ttl", TTL * 1000);//当一个消息被推送在该队列的时候 可以存在的时间 单位为ms，应小于队列过期时间  
            @event.Headers.Add("x-dead-letter-exchange", "amq.topic");//过期消息转向路由  
            @event.Headers.Add("x-dead-letter-routing-key",@event.EventTypeName);//过期消息转向路由相匹配routingkey 
        }

       
        /// <summary>
        /// 
        /// </summary>
        /// <param name="event"></param>
        /// <param name="expires">消息过期时间</param>
        public static void WithNoRetry(this EventLogEntry @event)
        {
            @event.Headers.Add("x-first-death-queue", $"{@event.EventTypeName}@Failed"); //死信队列名称
            @event.Headers.Add("x-dead-letter-exchange", "amq.topic");//过期消息转向路由  
            @event.Headers.Add("x-dead-letter-routing-key", @event.EventTypeName);//过期消息转向路由相匹配routingkey 

            //if (expires >0)
            //{
            //    @event.Headers.Add("x-expires", expires * 1000);//队列过期时间 
            //}
        }
        /// <summary>
        /// 不断重试（有等待时间，无重试次数限制)
        /// </summary>
        /// <param name="response"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static EventLogEntry RetryForever(this EventResponse response)
        {
            var numberOfRetries = 0;

            if (response.Headers.ContainsKey("x-message-retries"))
            {
                if (int.TryParse(response.Headers["x-message-retries"].ToString(), out numberOfRetries))
                {
                    numberOfRetries++;
                }
            }

            var @event = new Hummingbird.Extersions.EventBus.Models.EventLogEntry($"{response.QueueName}", response.Body);
            @event.WithRetry(0, numberOfRetries);
            return @event;

        }

        /// <summary>
        /// 不断重试（有等待时间，无重试次数限制)
        /// </summary>
        /// <param name="response"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static EventLogEntry WaitAndRetryForever(this EventResponse response,Func<int,int> func )
        {
            var numberOfRetries = 0;

            if (response.Headers.ContainsKey("x-message-retries"))
            {
                if (int.TryParse(response.Headers["x-message-retries"].ToString(), out numberOfRetries))
                {
                    numberOfRetries++;
                }
            }

            var ttl = func(numberOfRetries);
            var @event = new Hummingbird.Extersions.EventBus.Models.EventLogEntry($"{response.QueueName}", response.Body);
            @event.WithWaitSeconds(ttl);
            @event.WithRetry(0, numberOfRetries);
            return @event;

        }

        /// <summary>
        /// 重试，（有等待时间，有重试次数限制）
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static EventLogEntry WaitAndRetry(this EventResponse response,int maxRetries, Func<int, int> func)
        {
            var numberOfRetries = 0;

            if (response.Headers.ContainsKey("x-message-retries"))
            {
                if (int.TryParse(response.Headers["x-message-retries"].ToString(), out numberOfRetries))
                {
                    numberOfRetries++;
                }
            }

            var ttl = func(numberOfRetries);
            var @event = new Hummingbird.Extersions.EventBus.Models.EventLogEntry($"{response.QueueName}", response.Body);

            //当前重试次数小于最大重试次数
            if (numberOfRetries < maxRetries)
            {
                @event.WithWaitSeconds(ttl);
                @event.WithRetry(maxRetries, numberOfRetries);
            }
            else
            {
                @event.WithNoRetry();
            }

            return @event;
        }

        /// <summary>
        /// 重试，（有等待时间，有重试次数限制）
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static EventLogEntry NoRetry(this EventResponse response)
        {
            var @event = new Hummingbird.Extersions.EventBus.Models.EventLogEntry($"{response.QueueName}", response.Body);
            @event.WithNoRetry();
            return @event;
        }
    }
}
