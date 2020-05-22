using Hummingbird.Core;
using Hummingbird.Extensions.UidGenerator;
using Hummingbird.Extensions.UidGenerator.Abastracts;
using Hummingbird.Extensions.UidGenerator.WorkIdCreateStrategy;
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
                var workId = WorkIdCreateStrategy.NextId().Result;
                return new SnowflakeUniqueIdGenerator(workId, builder.CenterId);
            });

            return hostBuilder;
        }

        public static IWorkIdCreateStrategyBuilder AddStaticWorkIdCreateStrategy(this IWorkIdCreateStrategyBuilder hostBuilder, int WorkId)
        {
            hostBuilder.Services.AddSingleton<IWorkIdCreateStrategy>(sp =>
            {
                var strategy=  new StaticWorkIdCreateStrategy(WorkId);
                return strategy;
            });

            return hostBuilder;

        }

        public static IWorkIdCreateStrategyBuilder AddHostNameWorkIdCreateStrategy(this IWorkIdCreateStrategyBuilder hostBuilder)
        {
            hostBuilder.Services.AddSingleton<IWorkIdCreateStrategy>(sp =>
            {
                var strategy= new HostNameWorkIdCreateStrategy();
                return strategy;
            });

            return hostBuilder;
        }


    }

}
