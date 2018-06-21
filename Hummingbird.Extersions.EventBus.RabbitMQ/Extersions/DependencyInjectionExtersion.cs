
using Hummingbird.Extersions.EventBus;
using Hummingbird.Extersions.EventBus.Abstractions;
using Hummingbird.Extersions.EventBus.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public class RabbitMqOption
    {
        public string HostName { get; set; }
        public int Port { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public string VirtualHost { get; set; }

        public int RetryCount { get; set; }

        /// <summary>
        /// 幂等持续时间（秒）
        /// </summary>
        public int IdempotencyDuration { get; set; } = 15;
    }

    public static class DependencyInjectionExtersion
    {
        public static IHummingbirdEventBusHostBuilder AddRabbitmq(this IHummingbirdEventBusHostBuilder hostBuilder, Action<RabbitMqOption>  setupConnectionFactory)
        {
            setupConnectionFactory = setupConnectionFactory ?? throw new ArgumentNullException(nameof(setupConnectionFactory));

            var option = new RabbitMqOption();
            setupConnectionFactory(option);

            hostBuilder.Services.AddSingleton<IEventBus, EventBusRabbitMQ>(sp =>
            {
                var rabbitMQPersistentConnection = sp.GetRequiredService<IRabbitMQPersistentConnection>();
           
                var cache = sp.GetRequiredService<Hummingbird.Extersions.Cache.IHummingbirdCache<bool>>();
                var Configuration = sp.GetRequiredService<IConfiguration>();
                var logger = sp.GetRequiredService<ILogger<EventBusRabbitMQ>>();
                return new EventBusRabbitMQ(cache,
                    rabbitMQPersistentConnection, 
                    logger,
                    sp,
                    option.RetryCount);
            });

            hostBuilder.Services.AddSingleton<IRabbitMQPersistentConnection>(sp =>
            {
                    var Configuration = sp.GetRequiredService<IConfiguration>();
                    var logger = sp.GetRequiredService<ILogger<DefaultRabbitMQPersistentConnection>>(); 

                    var factory = new ConnectionFactory();
                    factory.HostName = option.HostName;
                    factory.Port = option.Port;
                    factory.Password = option.Password;
                    factory.UserName = option.UserName;
                    factory.VirtualHost = option.VirtualHost;

                    return new DefaultRabbitMQPersistentConnection(factory, logger, option.RetryCount);
                });

            return hostBuilder;
           
        }
    }
}
