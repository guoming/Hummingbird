using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Extersions.EventBus.RabbitMQ.LoadBalancers
{
    public class RoundRobinLoadBalancer :  IRabbitMQPersisterConnectionLoadBalancer
    {
        private readonly Func<List<IRabbitMQPersistentConnection>> _func;
        public RoundRobinLoadBalancer(Func<List<IRabbitMQPersistentConnection>> func)
        {
            this._func = func;

        }

        private readonly object _lock = new object();
        private int _last;

        public async Task<IRabbitMQPersistentConnection> Lease()
        {            
            var connection = _func();
            lock (_lock)
            {
                if (_last >= connection.Count)
                {
                    _last = 0;
                }

                var next = connection[_last];
                _last++;

                return next;
            }
        }
    }
}
