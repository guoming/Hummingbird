using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hummingbird.WebApi.Events
{
    public class NewMsgEventHandler : Hummingbird.Extersions.EventBus.Abstractions.IEventHandler<NewMsgEvent>
    {
        public Task<bool> Handle(NewMsgEvent @event)
        {
            Console.WriteLine(@event.Time);

            return Task.FromResult(true);
        }
    }
}
