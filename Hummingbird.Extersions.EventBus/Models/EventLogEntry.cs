using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Hummingbird.Extersions.EventBus;

namespace Hummingbird.Extersions.EventBus.Models
{
    public class EventLogEntry
    {
        private EventLogEntry() {
            this.CreationTime = DateTime.Now;
            this.State = EventStateEnum.NotPublished;
            this.TimesSent = 0;
        
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="EventTypeName">路由名称</param>
        /// <param name="event">消息主体</param>
        /// <param name="TTL">延期时间（秒）</param>
        public EventLogEntry(string EventTypeName, object @event) :this()
        {
            this.Headers = new Dictionary<string, object>();
            this.EventTypeName = string.IsNullOrEmpty(EventTypeName)? @event.GetType().FullName: EventTypeName;
            this.Content = JsonConvert.SerializeObject(@event);
            this.EventId = -1;
            this.MessageId = Guid.NewGuid().ToString("N");  
        }

        public static EventLogEntry Clone(EventResponse response)
        {
            return new EventLogEntry(response.QueueName, response.Body) {
                EventId = response.EventId,
                MessageId = response.MessageId,
                Headers= response.Headers,
            };

        }

 

       

        public IDictionary<string, object> Headers { get; set; }

        /// <summary>
        /// 事件编号
        /// </summary>
        public long EventId { get; set; }
    

        public string MessageId { get; set; }

  
        /// <summary>
        /// 事件类型
        /// </summary>
        public string EventTypeName { get; set; }
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
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// 内容
        /// </summary>
        public string Content { get; set; }
    }

    public class EventResponse
    {
        public string MessageId { get; set; }

        public long EventId { get; set; }

        public IDictionary<string, object> Headers { get; set; }

        public dynamic Body { get; set; }

        public string QueueName { get; set; }

        public string RouteKey { get; set; }    

    }

}
