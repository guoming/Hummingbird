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

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtersion
    {
        public static List<object> channels = new List<object>();




        public static IHummingbirdHostBuilder AddEventBus(this IHummingbirdHostBuilder hostBuilder, Action<IHummingbirdEventBusHostBuilder> setup)
        {
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
            eventBus.Subscribe(async (eventIds, queueName) =>
           {
               //消息消费成功执行以下代码
               if (eventIds.Length > 0)
               {
                   //出现异常则重试3次
                   await policy.ExecuteAsync(async (cancllationToken) =>
                  {
                       //这里可能会重复执行要保持幂等
                       await eventLogService.MarkEventConsumeAsRecivedAsync(eventIds, queueName, cancllationToken);

                      return await Task.FromResult(true);

                  }, CancellationToken.None);
               }

           }, async (eventIds, queueName, outEx, eventObjs) =>
           {
               //消息消费失败执行以下代码
               if (outEx != null)
               {
                   logger.LogError(outEx, outEx.Message);
               }

               if (eventIds.Length > 0)
               {
                   //使用重试策略执行，出现错误立即重试3次
                   return await policy.ExecuteAsync(async (cancellationToken) =>
                  {
                       //这里可能会重复，需要保持幂等
                       var times = await eventLogService.MarkEventConsumeAsFailedAsync(eventIds, queueName, cancellationToken);

                       //记录重试次数(在阀值内则重新写队列)
                       if (times > 3)
                      {
                          return false;
                      }
                      else
                      {
                          return true;
                      }
                  }, CancellationToken.None);
               }

               return true;

           });

            setupSubscriberHandler(eventBus);

            return serviceProvider;
        }


        public static IEventBus Register<TD, TH>(this IEventBus eventBus, string EventTypeName = "")
                     where TD : class
                     where TH : IEventHandler<TD>
        {
            return eventBus.Register<TD, TH>(EventTypeName);
        }

        public static IEventBus Register<TD, TH>(this IEventBus eventBus, string EventTypeName = "", int BatchSize = 10)
             where TD : class
             where TH : IEventBatchHandler<TD>
        {
            return eventBus.RegisterBatch<TD, TH>(EventTypeName, BatchSize);
        }
        /// <summary>
        /// 使用消息总线发布者
        /// 作者：郭明
        /// 日期：2017年11月21日
        /// </summary>
        public static async Task UseDispatcherAsync(this IServiceProvider serviceProvider, int TakeCount = 1000)
        {
            var eventBus = serviceProvider.GetRequiredService<IEventBus>();
            var logger = serviceProvider.GetRequiredService<ILogger<IEventLogger>>();
            var eventLogService = serviceProvider.GetRequiredService<IEventLogger>();
            //获取没有发布的事件列表
            var unPublishedEventList = eventLogService.GetUnPublishedEventList(TakeCount);
            //通过消息总线发布消息
            await eventBus.PublishAsync(unPublishedEventList, (events) =>
            {
                eventLogService.MarkEventAsPublishedAsync(events, CancellationToken.None);

            }, events =>
            {
                eventLogService.MarkEventAsPublishedFailedAsync(events, CancellationToken.None);

            }, events =>
            {
                eventLogService.MarkEventAsPublishedFailedAsync(events, CancellationToken.None);
            });
        }
    }
}
