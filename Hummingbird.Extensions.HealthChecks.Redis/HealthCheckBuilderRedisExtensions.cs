using StackExchange.Redis;
using System;
using System.Linq;

namespace Microsoft.Extensions.HealthChecks
{
    public static class HealthCheckBuilderRedisExtensions
    {
        public static HealthCheckBuilder AddRedisCheck(this HealthCheckBuilder builder, string name, string connectionString)
        {
            Guard.ArgumentNotNull(nameof(builder), builder);

            return AddRedisCheck(builder, name, builder.DefaultCacheDuration, connectionString);
        }



        public static HealthCheckBuilder AddRedisCheck(this HealthCheckBuilder builder, string name, TimeSpan cacheDuration, string connectionString)
        {
            builder.AddCheck($"RedisCheck({name})", () =>
            {
                try
                {
                    ConnectionMultiplexer connect = ConnectionMultiplexer.Connect(new ConfigurationOptions() { });
                    var response = connect.GetStatus();

                    if (response != null && response.Any())
                    {
                        return HealthCheckResult.Healthy($"RedisCheck({name}): Healthy");
                    }
                    return HealthCheckResult.Unhealthy($"RedisCheck({name}): Unhealthy");
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy($"RedisCheck({name}): Exception during check: {ex.GetType().FullName}");
                }
            }, cacheDuration);

            return builder;
        }
    }
}