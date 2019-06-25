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
           
            return Task.FromResult(true);
        }
    }

    public class NewMsgEventHandler2 : Hummingbird.Extersions.EventBus.Abstractions.IEventHandler<NewMsgEvent>
    {
        public NewMsgEventHandler2(IServiceProvider serviceProvider)
        {

        }

        public Task<bool> Handle(NewMsgEvent @event, System.Threading.CancellationToken cancellationToken)
        {
            Console.WriteLine(@event.Value.ToString());

            return Task.FromResult(true);
        }
    }
}
