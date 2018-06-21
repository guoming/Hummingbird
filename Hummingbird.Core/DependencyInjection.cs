using Hummingbird.Core;
using Microsoft.AspNetCore.Builder;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjection
    {
        /// <summary>
        /// 使用服务注册
        /// 作者：郭明
        /// 日期：2017年10月30日
        /// </summary>
        /// <param name="app"></param>
        /// <param name="configuration"></param>
        public static IServiceCollection AddHummingbird(this IServiceCollection services,Action<IHummingbirdHostBuilder> setup)
        {
            var builder= new HummingbirdHostBuilder(services);
            setup(builder);
            return services;
        }

        public static IHummingbirdApplicationBuilder UseHummingbird(this IApplicationBuilder app, Action<IHummingbirdApplicationBuilder> setup)
        {
            var builder = new HummingbirdApplicationBuilder(app);
            setup(builder);
            return builder;
        }

    }
}
