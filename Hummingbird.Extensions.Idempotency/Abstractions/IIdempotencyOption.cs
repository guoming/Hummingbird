using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extensions.Idempotency
{
    public interface IIdempotencyOption
    {
        /// <summary>
        /// 幂等持续时间(默认5分钟)
        /// </summary>
        TimeSpan Druation { get; set; }

        /// <summary>
        /// 缓存区域（默认：Idempotency）
        /// </summary>
        string CacheRegion { get; set; }
    }
}
