using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Hummingbird.Extersions.EventBus;

namespace Hummingbird.Extersions.EventBus.Models
{
    public class EventLogEntry
    {
        private EventLogEntry() { }     

        public EventLogEntry(object @event)
        {
            CreationTime =DateTime.Now;
            State = EventStateEnum.NotPublished;
            TimesSent = 0;
            EventTypeName = @event.GetType().FullName;
            Content = JsonConvert.SerializeObject(@event);
            EventId = Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// 时间编号
        /// </summary>
        public string EventId { get; private set; }
        /// <summary>
        /// 事件类型
        /// </summary>
        public string EventTypeName { get; private set; }
        /// <summary>
        /// 状态
        /// </summary>
        public EventStateEnum State { get; set; }
        /// <summary>
        /// 发送次数
        /// </summary>
        public int TimesSent { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreationTime { get; private set; }

        /// <summary>
        /// 内容
        /// </summary>
        public string Content { get; private set; }
    }
}
