using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Idempotency
{
    public interface IIdempotencyOption
    {
        /// <summary>
        /// 幂等持续时间
        /// </summary>
        TimeSpan Druation { get; set; }
        
        string IdempotencyRegion { get; set; }


    }
}
