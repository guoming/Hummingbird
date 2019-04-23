
using Hummingbird.Extersions.EventBus;
using Hummingbird.Extersions.EventBus.Abstractions;
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
        }


        /// <summary>
        /// 消费端设置
        /// </summary>
        /// <param name="ReceiverMaxConnections">消费最大连接数</param>
        /// <param name="AcquireRetryAttempts">最大重试次数</param>
        /// <param name="IdempotencyDurationSeconds">幂等持续时间（秒）</param>
        /// <param name="PreFetch">预取数量</param>
        public void WithReceiver(int ReceiverMaxConnections = 2, int AcquireRetryAttempts = 3, string LoadBalancer= "RoundRobinLoadBalancer", int IdempotencyDurationSeconds=15, ushort PreFetch=1)
        {
            this.ReceiverMaxConnections = ReceiverMaxConnections;
            this.ReceiverAcquireRetryAttempts = AcquireRetryAttempts;
            this.IdempotencyDuration = IdempotencyDuration;
            this.PreFetch = PreFetch;
            
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

        internal int ReceiverMaxConnections { get; set; } = 2;
        internal int ReceiverAcquireRetryAttempts { get; set; } = 3;
        internal string ReceiverLoadBalancer { get; set; }

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
                var cache = sp.GetRequiredService<Hummingbird.Extersions.Cache.IHummingbirdCache<bool>>();                
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
                    retryCount: option.SenderAcquireRetryAttempts,
                    preFetch:option.PreFetch,
                    IdempotencyDuration: option.IdempotencyDuration,
                    exchange: option.Exchange,
                    exchangeType: option.ExchangeType
                    );
            });

            return hostBuilder;
           
        }
    }
}
