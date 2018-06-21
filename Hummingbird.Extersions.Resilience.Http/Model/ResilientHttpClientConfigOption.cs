using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extersions.Resilience.Http
{
    public class ResilientHttpClientConfigOption
    {
        /// <summary>
        /// 超时时间(毫秒)
        /// </summary>
        public int TimeoutMillseconds { get; set; }

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// 最大允许的异常次数，超过则自动熔断
        /// </summary>
        public int ExceptionsAllowedBeforeBreaking { get; set; }

        /// <summary>
        /// 熔断持续的秒数
        /// </summary>
        public int DurationSecondsOfBreak { get; set; }
    }
}
