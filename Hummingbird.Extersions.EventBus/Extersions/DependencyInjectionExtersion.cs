using Hummingbird.Core;
using Hummingbird.Extersions.EventBus;
using Hummingbird.Extersions.EventBus.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtersion
    {
        public static List<object> channels = new List<object>();




        public static IHummingbirdHostBuilder AddEventBus(this IHummingbirdHostBuilder hostBuilder, Action<IHummingbirdEventBusHostBuilder> setup)
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                       .SelectMany(a => a.GetTypes().Where(type => Array.Exists(type.GetInterfaces(), t => t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(IEventHandler<>) || t.GetGenericTypeDefinition() == typeof(IEventBatchHandler<>)))))
                       .ToArray();
      
            foreach (var type in types)
            {
                hostBuilder.Services.AddSingleton(type);
            }

            var builder = new HummingbirdEventBusHostBuilder(hostBuilder.Services); ;
            setup(builder);
           
            return hostBuilder;
        }


        public static IHummingbirdApplicationBuilder UseEventBus(this IHummingbirdApplicationBuilder hummingbirdApplicationBuilder, Action<IServiceProvider> setupSubscriberHandler)
        {

            setupSubscriberHandler(hummingbirdApplicationBuilder.app.ApplicationServices);

            return hummingbirdApplicationBuilder;
        }

        private static IAsyncPolicy createPolicy(ILogger<IEventLogger> logger) {

            
            IAsyncPolicy policy = Policy.NoOpAsync();
            // 设置超时
            policy = Policy.TimeoutAsync(
                TimeSpan.FromMilliseconds(500),
                TimeoutStrategy.Pessimistic,
                (context, timespan, task) =>
                {
                    return Task.FromResult(true);
                }).WrapAsync(policy);

            //设置重试策略
            policy = Policy.Handle<Exception>()
                   .RetryAsync(3, (ex, time) =>
                   {
                       logger.LogError(ex, ex.ToString());
                   }).WrapAsync(policy);


            //设置熔断策略
            policy = policy.WrapAsync(Policy.Handle<Exception>()
                .AdvancedCircuitBreakerAsync(
                    failureThreshold: 0.5, // Break on >=50% actions result in handled exceptions...
                    samplingDuration: TimeSpan.FromSeconds(10), // ... over any 10 second period
                    minimumThroughput: 8, // ... provided at least 8 actions in the 10 second period.
                    durationOfBreak: TimeSpan.FromSeconds(30), // Break for 30 seconds.
                    onBreak: (Exception ex, TimeSpan timeSpan) =>
                    {
                        logger.LogError(ex, ex.ToString());
                    },
                    onHalfOpen: () =>
                    {

                    },
                    onReset: () =>
                    {

                    }));



            return policy;

        }


        /// <summary>
        /// 使用消息总线订阅者
        /// 作者：郭明
        /// 日期：2017年11月21日
        /// </summary>
        /// <param name="app"></param>
        public static IServiceProvider UseSubscriber(this IServiceProvider serviceProvider, Action<IEventBus> setupSubscriberHandler)
        {
            var eventBus = serviceProvider.GetRequiredService<IEventBus>();
            var logger = serviceProvider.GetRequiredService<ILogger<IEventLogger>>();
            var eventLogService = serviceProvider.GetRequiredService<IEventLogger>();
            //消息处理的策略(降级时返回处理失败)
            var policy = Policy<Boolean>.Handle<Exception>().FallbackAsync(false).WrapAsync(createPolicy(logger));


            //订阅消息
            eventBus.Subscribe((eventIds, queueName) =>
           {
              
              

           }, (eventIds, queueName, outEx, eventObjs) =>
           {
               //消息消费失败执行以下代码
               if (outEx != null)
               {
                   logger.LogError(outEx, outEx.Message);
               }

               return Task.FromResult(true);

           });

       
            setupSubscriberHandler(eventBus);
           

            return serviceProvider;
        }


        public static IEventBus Register<TD, TH>(this IEventBus eventBus,string QueueName="", string EventTypeName = "")
                     where TD : class
                     where TH : IEventHandler<TD>
        {
            return eventBus.Register<TD, TH>(EventTypeName);
        }

        public static IEventBus Register<TD, TH>(this IEventBus eventBus, string QueueName="", string EventTypeName = "", int BatchSize = 10)
             where TD : class
             where TH : IEventBatchHandler<TD>
        {
            return eventBus.RegisterBatch<TD, TH>(QueueName,EventTypeName, BatchSize);
        }       
    }
}
