using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hummingbird.WebApi.Events
{
    public class NewMsgEventHandler : Hummingbird.Extersions.EventBus.Abstractions.IEventBatchHandler<NewMsgEvent>
    {
        public Task<bool> Handle(NewMsgEvent[] @event, System.Threading.CancellationToken cancellationToken)
        {   
            return Task.FromResult(true);
        }
    }
}
