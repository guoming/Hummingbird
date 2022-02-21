using Hummingbird.Extensions.EventBus.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hummingbird.Example.Events.CanalEvent
{
    public class CanalEntryEventHandler : IEventBatchHandler<CanalEntryEvent>
    {
        private readonly ILogger<CanalEntryEventHandler> logger;

        public CanalEntryEventHandler(
            ILogger<CanalEntryEventHandler> logger)
        {
            this.logger = logger;

        }

        public Task<bool> Handle(CanalEntryEvent[] @events, Dictionary<string, object>[] Headers, CancellationToken cancellationToken)
        {

            return Task.FromResult(false);
        }
    }
}
