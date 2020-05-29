using Hummingbird.Extensions.EventBus.MySqlLogging;
using Microsoft.Extensions.DependencyInjection;
using Hummingbird.Core;
using Hummingbird.Extensions.EventBus;
using Hummingbird.Extensions.EventBus.Abstractions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtersion
    {
        public static IHummingbirdEventBusHostBuilder AddMySqlEventLogging(this IHummingbirdEventBusHostBuilder hostBuilder, Action<MySqlConfiguration> setupFactory)
        {
            #region 配置
            setupFactory = setupFactory ?? throw new ArgumentNullException(nameof(setupFactory));
            var configuration = new MySqlConfiguration();
            setupFactory(configuration);
            #endregion

            hostBuilder.Services.AddTransient<MySqlConfiguration>(a => configuration);
            hostBuilder.Services.AddTransient<IDbConnectionFactory>(a => new DbConnectionFactory(configuration.ConnectionString));
            hostBuilder.Services.AddTransient<IEventLogger, MySqlEventLogger>();
            return hostBuilder;
        }
    }
}
