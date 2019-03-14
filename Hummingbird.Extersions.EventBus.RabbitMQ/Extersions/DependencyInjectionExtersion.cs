
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
        /// 服务器地址(默认:localhost)
        /// </summary>
        public string HostName { get; set; } = "locahost";
        /// <summary>
        /// 端口（默认：5672）
        /// </summary>
        public int Port { get; set; } = 5672;

        /// <summary>
        /// 账号(默认:guest)
        /// </summary>
        public string UserName { get; set; } = "guest";

        /// <summary>
        /// 密码(默认:guest)
        /// </summary>
        public string Password { get; set; } = "guest";

        /// <summary>
        /// 虚拟主机(默认：/)
        /// </summary>
        public string VirtualHost { get; set; } = "/";

        /// <summary>
        /// 重试次数(默认：3)
        /// </summary>
        public int RetryCount { get; set; } = 3;

        /// <summary>
        /// 默认获取
        /// </summary>
        public ushort PreFetch { get; set; } = 1;

        /// <summary>
        /// 幂等持续时间（默认:15秒）
        /// </summary>
        public int IdempotencyDuration { get; set; } = 15;

        /// <summary>
        /// 交换机名称(默认：amq.topic)
        /// </summary>
        public string Exchange { get; set; } = "amq.topic";

        /// <summary>
        /// 交换机类型（默认：topic）
        /// </summary>
        public string ExchangeType { get; set; } = "topic";

        public int SenderConnectionPoolSize { get; set; } = 10;

        public int ReceiveConnectionPoolSize { get; set; } = 2;
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
                for (int i = 0; i < option.ReceiveConnectionPoolSize; i++)
                {
                    //消费端的连接池
                    receiveConnections.Add(new DefaultRabbitMQPersistentConnection(connectionFactory, loggerConnection, option.RetryCount));
                }

                //发送端连接池
                for (int i = 0; i < option.SenderConnectionPoolSize; i++)
                {
                    senderConnections.Add(new DefaultRabbitMQPersistentConnection(connectionFactory, loggerConnection, option.RetryCount));
                }

                var receiveLoadBlancer = rabbitMQPersisterConnectionLoadBalancerFactory.Get(()=> receiveConnections, "RoundRobinLoadBalancer");
                var senderLoadBlancer = rabbitMQPersisterConnectionLoadBalancerFactory.Get(()=> senderConnections, "RoundRobinLoadBalancer");

                return new EventBusRabbitMQ(cache,
                    receiveLoadBlancer,
                    senderLoadBlancer,
                    logger,
                    sp,
                    retryCount: option.RetryCount,
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
