using Hummingbird.EventBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Hummingbird.EventBus.SqlServer.Extersions
{
    public static class DependencyInjectionExtersion
    {
        public static IServiceCollection AddEventBusSqlServer(this IServiceCollection services,string ConnectionString)
        {
            services.AddTransient<IDbConnectionFactory>(a=>new DbConnectionFactory(ConnectionString));
            services.AddTransient<IEventLogService, EventLogService>();
            return services;
        }
    }
}
