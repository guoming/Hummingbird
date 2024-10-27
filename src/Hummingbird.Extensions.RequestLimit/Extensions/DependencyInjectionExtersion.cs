using Hummingbird.Core;
using System;
using Hummingbird.Extensions.RequestLimit;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtersion
    {

        public static IHummingbirdHostBuilder AddRequestLimit(this IHummingbirdHostBuilder hostBuilder, Action<IHummingbirdRequestLimitBuilder> action)
        {
            var builder= new HummingbirdRequestLimitBuilder(hostBuilder.Services);
            action(builder);            
            return hostBuilder;
        }
        
        public static IHummingbirdRequestLimitBuilder AddRateLimit(this IHummingbirdRequestLimitBuilder builder, IConfiguration configuration)
        {
            builder.Services.AddSingleton<RequestRateLimitConfiguration>(configuration.Get<RequestRateLimitConfiguration>());
            return builder;
        }
        
        public static IHummingbirdRequestLimitBuilder AddTimeoutLimit(this IHummingbirdRequestLimitBuilder builder, IConfiguration configuration)
        {
            builder.Services.AddSingleton<RequestTimeoutConfiguration>(configuration.Get<RequestTimeoutConfiguration>());
            return builder;
        }
        
        public static IApplicationBuilder UseRequestLimit(this IApplicationBuilder hostBuilder)
        {
            hostBuilder.UseMiddleware<RequestRateLimitMiddleware>();
            hostBuilder.UseMiddleware<RequestTimeoutMiddleware>();
            return hostBuilder;
        }


    }
}
