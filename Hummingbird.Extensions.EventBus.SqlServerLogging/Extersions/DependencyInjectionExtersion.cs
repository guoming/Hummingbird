using Hummingbird.Extensions.EventBus.SqlServerLogging;
using Microsoft.Extensions.DependencyInjection;
using Hummingbird.Core;
using Hummingbird.Extensions.EventBus;
using Hummingbird.Extensions.EventBus.Abstractions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtersion
    {
        public static IHummingbirdEventBusHostBuilder AddSqlServerEventLogging(this IHummingbirdEventBusHostBuilder hostBuilder, Action<SqlServerConfiguration> setupFactory)
        {
            #region 配置
            setupFactory = setupFactory ?? throw new ArgumentNullException(nameof(setupFactory));
            var configuration = new SqlServerConfiguration();
            setupFactory(configuration);
            #endregion

            hostBuilder.Services.AddTransient<SqlServerConfiguration>(a => configuration);
            hostBuilder.Services.AddTransient<IDbConnectionFactory>(a => new DbConnectionFactory(configuration.ConnectionString));
            hostBuilder.Services.AddTransient<IEventLogger, SqlServerEventLogger>();
            return hostBuilder;
        }
    }
}
