using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hummingbird.Example.Events.MongoShark
{
    public class MongodbSharkEventHandler : Hummingbird.Extensions.EventBus.Abstractions.IEventBatchHandler<MongodbSharkEvent>
    {
        public MongodbSharkEventHandler()
        {
        }

        public Task<bool> Handle(MongodbSharkEvent[] @event, Dictionary<string, object>[] Headers, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
