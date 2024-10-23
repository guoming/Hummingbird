using System.Collections.Generic;

namespace Hummingbird.Extensions.RequestLimit
{
    public class RequestTimeoutConfiguration
    {
        /// <summary>
        /// 超时规则
        /// </summary>
        public List<TimeoutRule> Rules { get; set; } = new List<TimeoutRule>();
    
        public class TimeoutRule
        {
            /// <summary>
            /// 路由名称
            /// </summary>
            public string Route { get; set; }
            
            /// <summary>
            /// 请求方式
            /// </summary>
            public string Method { get; set; }

            /// <summary>
            /// 超时毫秒
            /// </summary>
            public int TimeoutMillseconds { get; set; }
        }
        
    }
    
    
}