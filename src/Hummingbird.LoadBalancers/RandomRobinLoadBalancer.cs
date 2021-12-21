using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.LoadBalancers
{
    internal class RandomRobinLoadBalancer<T> : ILoadBalancer<T>
    {
        private readonly Func<List<T>> _func;
        private readonly List<T> _connections;

        public RandomRobinLoadBalancer(Func<List<T>> func)
        {
            this._func = func;
            this._connections = _func();

        }

        private readonly object _lock = new object();

        public T Lease()
        {            
            return Lease(_connections);
        }

        public T Lease(List<T> connections)
        {
            int _last = new Random(Guid.NewGuid().GetHashCode()).Next(connections.Count - 1);
            lock (_lock)
            {
                if (_last < connections.Count)
                {
                    _last = 0;
                }

                if (_last > connections.Count)
                {
                    _last = 0;
                }

                var next = connections[_last];

                return next;
            }
        }
    }
}
