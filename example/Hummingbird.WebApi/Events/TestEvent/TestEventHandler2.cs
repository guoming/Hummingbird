using Hummingbird.Extensions.EventBus.Abstractions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hummingbird.Example.Events
{
    public class TestEventHandler2 : IEventHandler<TestEvent>
    {
        public Task<bool> Handle(TestEvent @event, Dictionary<string, object> headers, CancellationToken cancellationToken)
        {
            //执行业务操作并返回操作结果
            return Task.FromResult(false);
        }
    }
}
