
using Hummingbird.Extensions.EventBus;
using Hummingbird.Extensions.EventBus.Abstractions;
using Hummingbird.Extensions.EventBus.Kafka;
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

        internal Confluent.Kafka.ConsumerConfig ConsumerConfig { get; set; }
        internal Confluent.Kafka.ProducerConfig ProducerConfig { get; set; }


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
        /// <param name="ReceiverAcquireRetryAttempts">最大重试次数</param>
        /// <param name="IdempotencyDurationSeconds">幂等持续时间（秒）</param>
        /// <param name="PreFetch">预取数量</param>
        public void WithReceiver(            
            int ReveiverMaxDegreeOfParallelism=1,
            int ReceiverAcquireRetryAttempts = 0, 
            int ReceiverHandlerTimeoutMillseconds=10000,
            string LoadBalancer= "RoundRobinLoadBalancer")
        {   
            this.ReceiverAcquireRetryAttempts = ReceiverAcquireRetryAttempts;
            this.ReceiverHandlerTimeoutMillseconds = ReceiverHandlerTimeoutMillseconds;
            this.ReveiverMaxDegreeOfParallelism= ReveiverMaxDegreeOfParallelism;
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
            int AcquireRetryAttempts = 3, 
            int SenderConfirmTimeoutMillseconds = 1000,
            int SenderConfirmFlushTimeoutMillseconds=50,
            string LoadBalancer = "RoundRobinLoadBalancer")
        {
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

        internal string SenderLoadBalancer { get; set; }

        internal int SenderConfirmTimeoutMillseconds { get; set; }= 500;

        internal int SenderConfirmFlushTimeoutMillseconds { get; set; }= 50;

        #endregion


        #region Receiver

   
        /// <summary>
        /// 消费者负载均衡器
        /// </summary>
        internal string ReceiverLoadBalancer { get; set; }

        internal int ReveiverMaxDegreeOfParallelism { get; set; } = 1;

        /// <summary>
        /// 消费者最大重试次数
        /// </summary>
        internal int ReceiverAcquireRetryAttempts { get; set; } = 3;

        /// <summary>
        /// 消费处理超时时间
        /// </summary>
        internal int ReceiverHandlerTimeoutMillseconds { get; set; } = 1000 * 2;
   
    

        #endregion

    }

    public static class DependencyInjectionExtersion
    {
        public static IHummingbirdEventBusHostBuilder AddKafka(this IHummingbirdEventBusHostBuilder hostBuilder, Action<KafkaOption> setupConnectionFactory)
        {
            return AddKafka(hostBuilder, "kafka",setupConnectionFactory);
        }

        public static IHummingbirdEventBusHostBuilder AddKafka(this IHummingbirdEventBusHostBuilder hostBuilder, string name,Action<KafkaOption>  setupConnectionFactory)
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
                return sp.GetRequiredService<EventBusKafka>();
            });
            
            hostBuilder.Services.AddSingleton<EventBusKafka>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IEventBus>>();
                var loggerConnection = sp.GetRequiredService<ILogger<IKafkaPersistentConnection>>();
                var rabbitMQPersisterConnectionLoadBalancerFactory = sp.GetRequiredService<ILoadBalancerFactory<IKafkaPersistentConnection>>();
                var senderConnections = new List<IKafkaPersistentConnection>();
                var receiveConnections = new List<IKafkaPersistentConnection>();

                if (option.ConsumerConfig != null)
                {
                    //消费端的连接池
                    receiveConnections.Add(new DefaultKafkaPersistentConnection(loggerConnection, option.ConsumerConfig));
                }

                if (option.ProducerConfig != null)
                {
                    //发送端连接池
                    senderConnections.Add(new DefaultKafkaPersistentConnection(loggerConnection, option.ProducerConfig));
                }

                var receiveLoadBlancer = rabbitMQPersisterConnectionLoadBalancerFactory.Get(()=> receiveConnections, option.ReceiverLoadBalancer);
                var senderLoadBlancer = rabbitMQPersisterConnectionLoadBalancerFactory.Get(()=> senderConnections, option.SenderLoadBalancer);

                var eventBus= new EventBusKafka(
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
                
                return eventBus;
            });

            return hostBuilder;
           
        }
    }

  
}
