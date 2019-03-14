using Hummingbird.Core;
using Hummingbird.Extersions.UidGenerator;
using Hummingbird.Extersions.UidGenerator.WorkIdCreateStrategy;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;


namespace Microsoft.Extensions.DependencyInjection
{
    public class IdGeneratorOption
    {

        /// <summary>
        /// 数据中心ID(默认0)
        /// </summary>
        public int CenterId { get; set; } = 0;

        /// <summary>
        /// 工作进程ID初始化策略
        /// </summary>
        internal IWorkIdCreateStrategy WorkIdCreateStrategy { get; set; }
    }

    public static class DependencyInjectionExtersion
    {
        public static IHummingbirdHostBuilder AddUniqueIdGenerator(this IHummingbirdHostBuilder hostBuilder, Action<IdGeneratorOption> setup)
        {
            var option = new IdGeneratorOption();
            setup(option);

            hostBuilder.Services.AddSingleton<IUniqueIdGenerator>(sp =>
            {
                var workId = option.WorkIdCreateStrategy.NextId();
                return new SnowflakeUniqueIdGenerator(workId, option.CenterId);
            });
            return hostBuilder;
        }

        public static void UseStaticWorkIdCreateStrategy(this IdGeneratorOption option, int WorkId)
        {
            option.WorkIdCreateStrategy = new StaticWorkIdCreateStrategy(WorkId);
        }
    }
}
