using Hummingbird.Extensions.EventBus.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extensions.EventBus.Kafka
{
    public static class EventLogEntryExtersions
    {

        /// <summary>
        /// 附加时间戳
        /// </summary>
        /// <param name="evnet"></param>
        /// <param name="ts"></param>
        public static void WithTimestamp(this EventLogEntry @evnet, long ts)
        {
            @evnet.Headers["x-ts"] = ts;
        }

        /// <summary>
        /// 附加时间戳
        /// </summary>
        /// <param name="evnet"></param>
        /// <param name="ts"></param>
        public static void WithTimestamp(this EventLogEntry @evnet)
        {
            @evnet.Headers["x-ts"] = DateTime.UtcNow.ToTimestamp();
        }

        public static void WithTracer(this EventLogEntry @evnet,string TraceId)
        {
            @evnet.Headers["x-traceId"] = TraceId;
        }

        /// <summary>
        /// 设置重试策略
        /// </summary>
        /// <param name="event"></param>
        /// <param name="MaxRetries">最大重试次数</param>
        /// <param name="NumberOfRetries">当前重试次数</param>
        /// <returns></returns>
        public static void WithRetry(this EventLogEntry @event, int MaxRetries,int NumberOfRetries)
        {
            
            @event.Headers["x-message-max-retries"]=MaxRetries;
            @event.Headers["x-message-retries"] = NumberOfRetries;

        }

        /// <summary>
        /// 设置延时策略
        /// </summary>
        /// <param name="event"></param>
        /// <param name="TTL">延时时间（秒）</param>
        /// <returns></returns>
        public static void WithWait(this EventLogEntry @event, int TTL)
        {
            @event.Headers["x-first-death-queue"]= $"{@event.EventTypeName}@Delay#{TTL}"; //死信队列名称
            @event.Headers["x-message-ttl"] = TTL * 1000; //当一个消息被推送在该队列的时候 可以存在的时间 单位为ms，应小于队列过期时间  
            @event.Headers["x-dead-letter-exchange"] = @event.Headers["x-exchange"];//过期消息转向路由  
            @event.Headers["x-dead-letter-routing-key"]= @event.EventTypeName;//过期消息转向路由相匹配routingkey 

     
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="event"></param>
        /// <param name="expires">消息过期时间</param>
        public static void WithNoRetry(this EventLogEntry @event)
        {
            @event.Headers["x-first-death-queue"]= $"{@event.EventTypeName}@Failed"; //死信队列名称
            @event.Headers.Remove("x-message-ttl");
            @event.Headers["x-dead-letter-exchange"] = @event.Headers["x-exchange"];//过期消息转向路由  
            @event.Headers["x-dead-letter-routing-key"] = @event.EventTypeName;//过期消息转向路由相匹配routingkey 
        }
        /// <summary>
        /// 不断重试（有等待时间，无重试次数限制)
        /// </summary>
        /// <param name="response"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static EventLogEntry RetryForever(this EventResponse response)
        {
            var @event = Hummingbird.Extensions.EventBus.Models.EventLogEntry.Clone(response);
            var numberOfRetries = response.GetNumberOfRetries();

            @event.WithRetry(0,++numberOfRetries);
            return @event;

        }

        /// <summary>
        /// 不断重试（有等待时间，无重试次数限制)
        /// </summary>
        /// <param name="response"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static EventLogEntry WaitAndRetryForever(this EventResponse response,  int TTL)
        {
            var @event = Hummingbird.Extensions.EventBus.Models.EventLogEntry.Clone(response);
            var numberOfRetries = response.GetNumberOfRetries();
            @event.WithWait(TTL);
            @event.WithRetry(0,++numberOfRetries);
            return @event;

        }

        /// <summary>
        /// 不断重试（有等待时间，无重试次数限制)
        /// </summary>
        /// <param name="response"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static EventLogEntry WaitAndRetryForever(this EventResponse response, Func<int, int> retryAttempt)
        {
            var numberOfRetries = response.GetNumberOfRetries();
            var ttl = retryAttempt(numberOfRetries);
            var @event = Hummingbird.Extensions.EventBus.Models.EventLogEntry.Clone(response);
            @event.WithWait(ttl);
            @event.WithRetry(0, ++numberOfRetries);
            return @event;

        }

        public static int GetNumberOfRetries(this EventResponse response)
        {
            var numberOfRetries = 0;
            if (response.Headers.ContainsKey("x-message-retries"))
            {
                int.TryParse(response.Headers["x-message-retries"].ToString(), out numberOfRetries);

            }

            return numberOfRetries;
        }

        /// <summary>
        /// 重试，（有等待时间，有重试次数限制）
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static EventLogEntry WaitAndRetry(this EventResponse response,int TTL, int maxRetries)
        {
            var @event = Hummingbird.Extensions.EventBus.Models.EventLogEntry.Clone(response);
            var numberOfRetries = response.GetNumberOfRetries();
            //当前重试次数小于最大重试次数
            if (numberOfRetries < maxRetries)
            {
                @event.WithWait(TTL);
                @event.WithRetry(maxRetries,++numberOfRetries);
            }
            else
            {
                @event.WithNoRetry();
            }

            return @event;
        }

        /// <summary>
        /// 重试，（有等待时间，有重试次数限制）
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static EventLogEntry WaitAndRetry(this EventResponse response, Func<int, int> retryAttempt, int maxRetries)
        {
            var @event = Hummingbird.Extensions.EventBus.Models.EventLogEntry.Clone(response);
            var numberOfRetries = response.GetNumberOfRetries();
            var TTL = retryAttempt(numberOfRetries);

            //当前重试次数小于最大重试次数
            if (numberOfRetries < maxRetries)
            {
                @event.WithWait(TTL);
                @event.WithRetry(maxRetries,++ numberOfRetries);
            }
            else
            {
                @event.WithNoRetry();
            }

            return @event;
        }

        /// <summary>
        /// 重试，（有等待时间，有重试次数限制）
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static EventLogEntry NoRetry(this EventResponse response)
        {
            var @event = Hummingbird.Extensions.EventBus.Models.EventLogEntry.Clone(response);
            @event.WithNoRetry();
            return @event;
        }
    }
}
