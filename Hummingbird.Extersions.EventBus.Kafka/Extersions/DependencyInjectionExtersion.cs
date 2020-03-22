
using Hummingbird.Extersions.EventBus;
using Hummingbird.Extersions.EventBus.Abstractions;
using Hummingbird.Extersions.EventBus.Kafka;
using Hummingbird.LoadBalancers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    public class KafkaOption
    {

        internal Confluent.Kafka.ConsumerConfig ConsumerConfig { get; set; } = new Confluent.Kafka.ConsumerConfig() { BootstrapServers = "localhost:9092" };
        internal Confluent.Kafka.ProducerConfig ProducerConfig { get; set; } = new Confluent.Kafka.ProducerConfig() { BootstrapServers = "localhost:9092" };


        public void WithReceiverConfig(Confluent.Kafka.ConsumerConfig config)
        {
            ConsumerConfig = config;
        }

        public void WithSenderConfig(Confluent.Kafka.ProducerConfig config)
        {
            ProducerConfig = config;
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
            int ReceiverHandlerTimeoutMillseconds=10000,
            string LoadBalancer= "RoundRobinLoadBalancer",
            ushort PreFetch=1)
        {
            this.ReceiverMaxConnections = ReceiverMaxConnections;
            this.ReveiverMaxDegreeOfParallelism = ReveiverMaxDegreeOfParallelism;
            this.ReceiverAcquireRetryAttempts = ReceiverAcquireRetryAttempts;
            this.ReceiverHandlerTimeoutMillseconds = ReceiverHandlerTimeoutMillseconds;
            this.PreFetch = PreFetch;
            this.ReceiverLoadBalancer = LoadBalancer;
            
        }

        /// <summary>
        /// 设置连接池信息
        /// </summary>
        /// <param name="SenderMaxConnections">发送端最大连接数量</param>
        /// <param name="ReceiverMaxConnections">消费端最大连接数量</param>
        /// <param name="SenderConfirmTimeoutMillseconds">消息确认超时时间（毫秒）</param>
        /// <param name="SenderConfirmFlushTimeoutMillseconds">消息刷新等待间隔时间（毫秒）</param>
        /// <param name="AcquireRetryAttempts">最大重试次数</param>
        public void WithSender(
            int SenderMaxConnections = 10, 
            int AcquireRetryAttempts = 3, 
            int SenderConfirmTimeoutMillseconds = 1000,
            int SenderConfirmFlushTimeoutMillseconds=50,
            string LoadBalancer = "RoundRobinLoadBalancer")
        {
            this.SenderMaxConnections = SenderMaxConnections;
            this.SenderAcquireRetryAttempts = AcquireRetryAttempts;
            this.SenderLoadBalancer = LoadBalancer;
            this.SenderConfirmTimeoutMillseconds = SenderConfirmTimeoutMillseconds;
            this.SenderConfirmFlushTimeoutMillseconds = SenderConfirmFlushTimeoutMillseconds;
        }




        #region Sender
        /// <summary>
        /// 重试次数(默认：3)
        /// </summary>
        internal int SenderAcquireRetryAttempts { get; set; } = 3;

        internal int SenderMaxConnections { get; set; } = 10;

        internal string SenderLoadBalancer { get; set; }

        internal int SenderConfirmTimeoutMillseconds { get; set; }= 500;

        internal int SenderConfirmFlushTimeoutMillseconds { get; set; }= 50;

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

        #endregion

    }

    public static class DependencyInjectionExtersion
    {
        public static IHummingbirdEventBusHostBuilder AddKafka(this IHummingbirdEventBusHostBuilder hostBuilder, Action<KafkaOption>  setupConnectionFactory)
        {
            setupConnectionFactory = setupConnectionFactory ?? throw new ArgumentNullException(nameof(setupConnectionFactory));

            var option = new KafkaOption();
            setupConnectionFactory(option);

            hostBuilder.Services.AddSingleton<ILoadBalancerFactory<IKafkaPersistentConnection>>(sp =>
            {
                return new DefaultLoadBalancerFactory<IKafkaPersistentConnection>();
            });
            hostBuilder.Services.AddSingleton<IEventBus, EventBusKafka>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IEventBus>>();
                var loggerConnection = sp.GetRequiredService<ILogger<IKafkaPersistentConnection>>();
                var rabbitMQPersisterConnectionLoadBalancerFactory = sp.GetRequiredService<ILoadBalancerFactory<IKafkaPersistentConnection>>();
                var senderConnections = new List<IKafkaPersistentConnection>();
                var receiveConnections = new List<IKafkaPersistentConnection>();

                //消费端连接池
                for (int i = 0; i < option.ReceiverMaxConnections; i++)
                {
                    var connection = new DefaultKafkaPersistentConnection(loggerConnection, option.ConsumerConfig);
                    //消费端的连接池
                    receiveConnections.Add(connection);
                }

                //发送端连接池
                for (int i = 0; i < option.SenderMaxConnections; i++)
                {
                    var connection = new DefaultKafkaPersistentConnection(loggerConnection, option.ProducerConfig);
                    senderConnections.Add(connection);
                }

                var receiveLoadBlancer = rabbitMQPersisterConnectionLoadBalancerFactory.Get(()=> receiveConnections, option.ReceiverLoadBalancer);
                var senderLoadBlancer = rabbitMQPersisterConnectionLoadBalancerFactory.Get(()=> senderConnections, option.SenderLoadBalancer);

                return new EventBusKafka(
                    receiveLoadBlancer,
                    senderLoadBlancer,
                    logger,
                    sp,
                    senderRetryCount: option.SenderAcquireRetryAttempts,
                    senderConfirmTimeoutMillseconds: option.SenderConfirmTimeoutMillseconds,
                    senderConfirmFlushTimeoutMillseconds: option.SenderConfirmFlushTimeoutMillseconds,
                    reveiverMaxDegreeOfParallelism: option.ReveiverMaxDegreeOfParallelism,
                    receiverAcquireRetryAttempts: option.ReceiverAcquireRetryAttempts,
                    receiverHandlerTimeoutMillseconds: option.ReceiverHandlerTimeoutMillseconds
                   );
            });

            return hostBuilder;
           
        }
    }

  
}
