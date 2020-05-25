using Consul;
using Hummingbird.Core;
using Hummingbird.DynamicRoute;
using Hummingbird.Extensions.UidGenerator.Abastracts;
using Hummingbird.Extensions.UidGenerator.WorkIdCreateStrategy;

namespace Microsoft.Extensions.DependencyInjection
{

    public static class DependencyInjectionExtersion
    {

        public static IWorkIdCreateStrategyBuilder AddConsulWorkIdCreateStrategy(this IWorkIdCreateStrategyBuilder hostBuilder, string AppId)
        {
            hostBuilder.Services.AddSingleton<IWorkIdCreateStrategy>(sp =>
            {
                var serviceDiscoveryProvider = sp.GetService<IServiceDiscoveryProvider>();
                var consulClient = sp.GetService<IConsulClient>();
                var strategy = new ConsulWorkIdCreateStrategy(serviceDiscoveryProvider, consulClient, AppId);
                return strategy;
            });

            return hostBuilder;
        }
    }

}
