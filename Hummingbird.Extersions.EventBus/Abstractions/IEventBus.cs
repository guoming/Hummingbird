using Hummingbird.Extersions.EventBus;
using Hummingbird.Extersions.EventBus.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Hummingbird.Extersions.EventBus.Abstractions
{
    /// <summary>
    /// 事件总线接口
    /// 作者：郭明
    /// 日期：2017年11月15日
    /// </summary>
    public interface IEventBus
    {
            Task PublishNonConfirmAsync(List<Models.EventLogEntry> Events, CancellationToken cancellationToken = default(CancellationToken));

   
            Task<bool> PublishAsync(List<Models.EventLogEntry> Events, CancellationToken cancellationToken=default(CancellationToken));

            /// <summary>
            /// 订阅消息（同一类消息可以重复订阅）
            /// 作者：郭明
            /// 日期：2017年4月3日
            /// </summary>
            /// <param name="QueueName">队列名称</param>     
            /// <param name="EventTypeName">事件类型名称</param>        
            IEventBus Register<TD, TH>(string QueueName = "", string EventTypeName = "", CancellationToken cancellationToken = default(CancellationToken))
              where TD : class
              where TH : IEventHandler<TD>;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TD"></typeparam>
        /// <typeparam name="TH"></typeparam>
        /// <param name="QueueName">队列名称</param>
        /// <param name="EventTypeName">事件类型名称</param>
        /// <param name="BatchSize">批量获取消息大小</param>
        /// <returns></returns>
        IEventBus RegisterBatch<TD, TH>(string QueueName = "", string EventTypeName = "", int BatchSize = 50, CancellationToken cancellationToken = default(CancellationToken))
               where TD : class
                 where TH : IEventBatchHandler<TD>;

        /// <summary>
        /// 订阅消息
        /// </summary>
        /// <param name="ackHandler"></param>
        /// <param name="nackHandler"></param>
        /// <returns></returns>
        IEventBus Subscribe(
        Action<EventResponse[]> ackHandler,
        Func<(EventResponse[] Messages, Exception Exception), Task<bool>> nackHandler);

    }


}
