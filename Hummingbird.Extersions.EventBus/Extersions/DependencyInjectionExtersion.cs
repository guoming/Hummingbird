using Hummingbird.Core;
using Hummingbird.Extersions.EventBus;
using Hummingbird.Extersions.EventBus.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtersion
    {
        public static List<object> channels = new List<object>();

        public static IHummingbirdHostBuilder AddEventBus(this IHummingbirdHostBuilder hostBuilder,Action<IHummingbirdEventBusHostBuilder> setup)
        {
            var builder = new HummingbirdEventBusHostBuilder(hostBuilder.Services); ;
            setup(builder);
            return hostBuilder;
        }


        public static IHummingbirdApplicationBuilder UseEventBus(this IHummingbirdApplicationBuilder hummingbirdApplicationBuilder, Action<IServiceProvider> setupSubscriberHandler) {

            setupSubscriberHandler(hummingbirdApplicationBuilder.app.ApplicationServices);

            return hummingbirdApplicationBuilder;
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

            //设置重试策略
            var policy = RetryPolicy.Handle<Exception>()
                   .Retry(3, (ex, time) =>
                   {
                       logger.LogError(ex, ex.ToString());
                   });

            //订阅消息
            eventBus.Subscribe((eventId, queueName) =>
           {
                //消息消费成功执行以下代码
                if (!string.IsNullOrEmpty(eventId))
               {
                    //出现异常则重试3次
                    policy.Execute(async () =>
                   {
                        //这里可能会重复执行要保持幂等
                        await eventLogService.MarkEventConsumeAsRecivedAsync(eventId, queueName);
                   });
               }

           }, async (eventId, queueName, outEx, eventObj) =>
           {
                //消息消费失败执行以下代码
                if (outEx != null)
               {
                   logger.LogError(outEx, outEx.Message);
               }

               if (!string.IsNullOrEmpty(eventId))
               {
                    //使用重试策略执行，出现错误立即重试3次
                    return await policy.Execute(async () =>
                   {
                        //这里可能会重复，需要保持幂等
                        var times = await eventLogService.MarkEventConsumeAsFailedAsync(eventId, queueName);

                        //记录重试次数(在阀值内则重新写队列)
                        if (times > 3)
                       {
                           return false;
                       }
                       else
                       {
                           return true;
                       }
                   });
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

        /// <summary>
        /// 使用消息总线发布者
        /// 作者：郭明
        /// 日期：2017年11月21日
        /// </summary>
        public static IServiceProvider UseDispatcher(this IServiceProvider serviceProvider, int TakeCount = 1000)
        {

            var eventBus = serviceProvider.GetRequiredService<IEventBus>();
            var logger = serviceProvider.GetRequiredService<ILogger<IEventLogger>>();
            var eventLogService = serviceProvider.GetRequiredService<IEventLogger>();
            //获取没有发布的事件列表
            var unPublishedEventList = eventLogService.GetUnPublishedEventList(TakeCount);
            //通过消息总线发布消息
            eventBus.Publish(unPublishedEventList, (events) =>
            {
                eventLogService.MarkEventAsPublishedAsync(events);

            }, events =>
            {
                eventLogService.MarkEventAsPublishedFailedAsync(events);

            }, events =>
            {
                eventLogService.MarkEventAsPublishedFailedAsync(events);
            });

            return serviceProvider;
        }
    }
}
