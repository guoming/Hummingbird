using Hummingbird.Core;
using Hummingbird.Extersions.Dapper.SqlServer;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtersion
    {
        public static IServiceCollection AddDapper(this IServiceCollection services, string ConnectionString)
        {
            services.AddTransient<IDbConnectionFactory>(a => new DbConnectionFactory(ConnectionString));
            return services;
        }
    }
}
