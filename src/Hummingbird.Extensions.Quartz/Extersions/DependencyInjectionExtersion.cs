using System;
using Hummingbird.Extensions.Quartz;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtersion
    {


        /// <summary>
        /// 配置后台运行程序
        /// </summary>
        /// <param name="builder"></param>
        public static IServiceCollection AddQuartz(this IServiceCollection services, IConfigurationSection configurationSection)
        {
            services.AddHostedService<CornJobSchedulerHostedService>();

            services.AddTransient<CornJobConfiguration>(sp =>
            {
                var logger = sp.GetService<ILogger<IConfiguration>>();
                var config = configurationSection.Get<CornJobConfiguration>();
                if (config == null)
                {
                    logger.LogWarning($"configuration section CornJob not found");
                    config = new CornJobConfiguration();
                }

                return config;
            })
            .AddQuartz(q => {
                // handy when part of cluster or you want to otherwise identify multiple schedulers
                q.SchedulerId = "Scheduler-Core";

                // we take this from appsettings.json, just show it's possible
                // q.SchedulerName = "Quartz ASP.NET Core Sample Scheduler";

                // as of 3.3.2 this also injects scoped services (like EF DbContext) without problems
                q.UseMicrosoftDependencyInjectionJobFactory();
                // or for scoped service support like EF Core DbContext
                //q.UseMicrosoftDependencyInjectionScopedJobFactory();

                // these are the defaults
                q.UseSimpleTypeLoader();
                q.UseInMemoryStore();
                q.UseDefaultThreadPool(tp =>
                {
                    tp.MaxConcurrency = 10;
                });

                // also add XML configuration and poll it for changes
                q.UseXmlSchedulingConfiguration(x =>
                {
                    x.Files = new[] { "~/quartz.config" };
                    x.ScanInterval = TimeSpan.FromSeconds(2);
                    x.FailOnFileNotFound = true;
                    x.FailOnSchedulingError = true;
                });

                // convert time zones using converter that can handle Windows/Linux differences
                //q.UseTimeZoneConverter();

                // auto-interrupt long-running job
                q.UseJobAutoInterrupt(options =>
                {
                    // this is the default
                    options.DefaultMaxRunTime = TimeSpan.FromMinutes(5);
                });

            })
            .AddSingleton<Quartz.IScheduler>((sp) => {

                var scheduler = StdSchedulerFactory.GetDefaultScheduler().Result;
                return scheduler;
            });
            return services;
        }
    }
}
