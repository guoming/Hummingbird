using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Extersions.Cacheing.LoadBalancers
{
    internal class RoundRobinLoadBalancer :  IConnectionLoadBalancer
    {
        private readonly List<StackExchangeImplement.RedisClientHelper> connections;
        public RoundRobinLoadBalancer(Func<List<StackExchangeImplement.RedisClientHelper>> func)
        {
            this.connections = func();

        }

        private readonly object _lock = new object();
        private int _last;

        public StackExchangeImplement.RedisClientHelper Lease()
        {            
         
            lock (_lock)
            {
                if (_last >= connections.Count)
                {
                    _last = 0;
                }

                var next = connections[_last];
                _last++;

                return next;
            }
        }
    }
}
