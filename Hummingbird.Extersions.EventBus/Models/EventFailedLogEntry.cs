using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extersions.EventBus.Models
{
    public class EventFailedLogEntry
    {
        /// <summary>
        /// 事件编号
        /// </summary>
        public long EventId { get; set; }

        /// <summary>
        /// 队列名称
        /// </summary>

        public string QueueName { get; set; }


        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreationTime { get; set; }
    }
}
