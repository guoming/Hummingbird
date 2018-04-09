using Hummingbird.EventBus;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hummingbird.EventBus.Abstractions
{
    /// <summary>
    /// 事件总线接口
    /// 作者：郭明
    /// 日期：2017年11月15日
    /// </summary>
    public interface IEventBus
    {

        /// <summary>
        /// 发送消息
        /// </summary>
        void Publish(
            List<Models.EventLogEntry> Events,
            Action<List<string>> ackHandler = null,
            Action<List<string>> nackHandler = null,
            Action<List<string>> returnHandler = null,
            int EventDelaySeconds = 0,
            int TimeoutMilliseconds = 500);

        /// <summary>
        /// 订阅消息（同一类消息可以重复订阅）
        /// 作者：郭明
        /// 日期：2017年4月3日
        /// </summary>
        /// <param name="EventTypeName">消息类型名称</param>        
        IEventBus Register<TD, TH>(string EventTypeName = "")
              where TD : EventEntity
              where TH : IEventHandler<TD>;

        /// <summary>
        /// 订阅消息
        /// </summary>
        /// <param name="ackHandler"></param>
        /// <param name="nackHandler"></param>
        /// <returns></returns>
        IEventBus Subscribe(
        Action<string, string> ackHandler,
        Func<string, string, Exception, dynamic, Task<bool>> nackHandler);

    }
}
