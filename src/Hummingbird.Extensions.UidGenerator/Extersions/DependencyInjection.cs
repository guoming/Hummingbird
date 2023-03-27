using Hummingbird.Core;
using Hummingbird.Extensions.UidGenerator;
using Hummingbird.Extensions.UidGenerator.Abastracts;
using Hummingbird.Extensions.UidGenerator.Implements;
using System;


namespace Microsoft.Extensions.DependencyInjection
{

    public static class DependencyInjectionExtersion
    {
   

        public static IHummingbirdHostBuilder AddSnowflakeUniqueIdGenerator(
            this IHummingbirdHostBuilder hostBuilder, 
            Action<IWorkIdCreateStrategyBuilder> workIdCreateStrategyBuilder)
        {
            
            var builder = new WorkIdCreateStrategyBuilder(hostBuilder.Services);
          
            workIdCreateStrategyBuilder(builder);

            hostBuilder.Services.AddSingleton<IUniqueIdGenerator>(sp =>
            {
                var WorkIdCreateStrategy = sp.GetService<IWorkIdCreateStrategy>();
                var workId = WorkIdCreateStrategy.GetWorkId().Result;
                return new SnowflakeUniqueIdGenerator(workId, WorkIdCreateStrategy.GetCenterId());
            });

#if NETCORE
            hostBuilder.Services.AddHostedService<InitWorkIdHostedService>();
#endif

            return hostBuilder;
        }

        public static IWorkIdCreateStrategy AddStaticWorkIdCreateStrategy(this IWorkIdCreateStrategyBuilder hostBuilder, int CenterId,int WorkId)
        {  
            var strategy=  new StaticWorkIdCreateStrategy(CenterId,WorkId);
            hostBuilder.Services.AddSingleton<IWorkIdCreateStrategy>(sp =>
            {
               
                return strategy;
            });

            return strategy;

        }

        public static IWorkIdCreateStrategy AddHostNameWorkIdCreateStrategy(this IWorkIdCreateStrategyBuilder hostBuilder,int CenterId)
        {
            var strategy= new HostNameWorkIdCreateStrategy(CenterId);
            hostBuilder.Services.AddSingleton<IWorkIdCreateStrategy>(sp =>
            {
               
                return strategy;
            });

            return strategy;
        }
    }

}
