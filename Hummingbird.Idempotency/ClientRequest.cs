using System;

namespace Hummingbird.Idempotency
{
    public class ClientRequest
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime RequestTime { get; set; }

        public DateTime ResponseTime { get; set; }

        /// <summary>
        /// 请求
        /// </summary>
        public string Request { get; set; }

        /// <summary>
        /// 相应
        /// </summary>
        public string Response { get; set; }
    }
}
