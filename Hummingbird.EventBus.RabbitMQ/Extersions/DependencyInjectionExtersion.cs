
using Autofac;
using Hummingbird.EventBus.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;

namespace Hummingbird.EventBus.RabbitMQ.Extersions
{
    public class RabbitMqConnectionOption
    {
        public string HostName { get; set; }
        public int Port { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public string VirtualHost { get; set; }

        public int RetryCount { get; set; }
    }

    public static class DependencyInjectionExtersion
    {
        public static void AddEventBusRabbitmq(this IServiceCollection services, Action<RabbitMqConnectionOption>  setupConnectionFactory)
        {
            setupConnectionFactory = setupConnectionFactory ?? throw new ArgumentNullException(nameof(setupConnectionFactory));

            var option = new RabbitMqConnectionOption();
            setupConnectionFactory(option);

            services.AddSingleton<IEventBus, EventBusRabbitMQ>(sp =>
            {
                var rabbitMQPersistentConnection = sp.GetRequiredService<IRabbitMQPersistentConnection>();
                var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                var Configuration = sp.GetRequiredService<IConfiguration>();
                var logger = sp.GetRequiredService<ILogger<EventBusRabbitMQ>>();
                return new EventBusRabbitMQ(rabbitMQPersistentConnection, logger, iLifetimeScope, option.RetryCount);
            });

            services.AddSingleton<IRabbitMQPersistentConnection>(sp =>
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
        }
    }
}
