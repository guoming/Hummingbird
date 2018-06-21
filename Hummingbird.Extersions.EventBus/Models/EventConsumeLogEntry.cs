using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extersions.EventBus.Models
{
    public class EventConsumeLogEntry
    {
        public string EventConsumeLogId { get; set; }

        /// <summary>
        /// 事件编号
        /// </summary>
        public string EventId { get; set; }

        /// <summary>
        /// 队列名称
        /// </summary>

        public string QueueName { get; set; }

        /// <summary>
        /// 消费次数
        /// </summary>
        public int TimesConsume { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public EventConsumeStateEnum State { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreationTime { get; set; }
    }
}
