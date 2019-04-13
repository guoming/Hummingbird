using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hummingbird.WebApi.Events
{
    public class NewMsgEventBatchHandler : Hummingbird.Extersions.EventBus.Abstractions.IEventBatchHandler<NewMsgEvent>
    {
        public Task<bool> Handle(NewMsgEvent[] @event, System.Threading.CancellationToken cancellationToken)
        {
            foreach (var item in @event)
            {
                Console.WriteLine(item.Time.ToString());
            }
            return Task.FromResult(true);
        }
    }

    public class NewMsgEventHandler : Hummingbird.Extersions.EventBus.Abstractions.IEventHandler<NewMsgEvent>
    {
        public NewMsgEventHandler(IServiceProvider serviceProvider)
        {

        }

        public Task<bool> Handle(NewMsgEvent @event, System.Threading.CancellationToken cancellationToken)
        {
            Console.WriteLine(@event.Time.ToString());

            return Task.FromResult(true);
        }
    }
}
