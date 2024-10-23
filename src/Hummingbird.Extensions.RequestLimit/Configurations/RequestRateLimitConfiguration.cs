using System.Collections.Generic;

namespace Hummingbird.Extensions.RequestLimit
{
    public class RequestRateLimitConfiguration
    {
        /// <summary>
        /// 限流规则
        /// </summary>
        public List<RateLimitRule> Rules { get; set; } = new List<RateLimitRule>();
    
        public class RateLimitRule
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
            /// 请求数量
            /// </summary>
            public int NumberOfRequests { get; set; }
            
            /// <summary>
            /// 时间间隔秒
            /// </summary>
            public int PeriodInSeconds { get; set; }
            
            /// <summary>
            /// 最大突发流量
            /// </summary>
            public int MaxBurst { get; set; } = 0;
        }
        
    }
    
    
}