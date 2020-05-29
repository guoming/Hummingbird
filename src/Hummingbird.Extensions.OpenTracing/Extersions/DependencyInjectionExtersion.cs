using Hummingbird.Core;
using Hummingbird.Extensions.Tracing;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtersion
    {

        public static IHummingbirdHostBuilder AddOpenTracing(this IHummingbirdHostBuilder hostBuilder, Action<IHummingbirdOpenTracingBuilder> action)
        {
            var builder= new HummingbirdOpenTracingBuilder(hostBuilder.Services);
            action(builder);            
            return hostBuilder;
        } 
    }
}
