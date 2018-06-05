using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Idempotency
{
    public class IdempotencyOption : IIdempotencyOption
    {
        public TimeSpan Druation { get; set; } = TimeSpan.FromMinutes(5);

        public string IdempotencyRegion { get; set; } = "Idempotency";
    }
}
