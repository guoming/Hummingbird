using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extersions.EventBus.Models
{
    public enum EventStateEnum
    {
        NotPublished = 0,
        Published = 1,
        PublishedFailed = 2
    }
}
