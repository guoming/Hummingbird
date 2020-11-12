using Hummingbird.Extensions.EventBus.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hummingbird.Example.Events
{

    public class TestEventHandler1 : IEventHandler<TestEvent>,IEventBatchHandler<TestEvent>
    {
        public Task<bool> Handle(TestEvent @event, Dictionary<string, object> headers, CancellationToken cancellationToken)
        {
            //执行业务操作并返回操作结果
            return Task.FromResult(true);
        }

        public Task<bool> Handle(TestEvent[] @event, Dictionary<string, object>[] Headers, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }
}
