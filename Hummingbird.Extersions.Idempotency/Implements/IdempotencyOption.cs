using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extersions.Idempotency
{
    public class IdempotencyOption : IIdempotencyOption
    {
        public TimeSpan Druation { get; set; } = TimeSpan.FromMinutes(5);

        public string CacheRegion { get; set; } = "Idempotency";
    }
}
