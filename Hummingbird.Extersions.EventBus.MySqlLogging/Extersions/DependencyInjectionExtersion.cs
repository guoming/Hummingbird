using Hummingbird.Extersions.EventBus.MySqlLogging;
using Microsoft.Extensions.DependencyInjection;
using Hummingbird.Core;
using Hummingbird.Extersions.EventBus;
using Hummingbird.Extersions.EventBus.Abstractions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtersion
    {
        public static IHummingbirdEventBusHostBuilder AddMySqlEventLogging(this IHummingbirdEventBusHostBuilder hostBuilder, string ConnectionString)
        {
            
            hostBuilder.Services.AddTransient<IDbConnectionFactory>(a => new DbConnectionFactory(ConnectionString));
            hostBuilder.Services.AddTransient<IEventLogger, MySqlEventLogger>();
            return hostBuilder;
        }
    }
}
