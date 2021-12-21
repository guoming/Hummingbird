using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.LoadBalancers
{
    internal class RoundRobinLoadBalancer<T> : ILoadBalancer<T>
    {
        private readonly Func<List<T>> _func;
        private readonly List<T> _connections;
        public RoundRobinLoadBalancer(Func<List<T>> func)
        {
            this._func = func;
            this._connections = _func();
        }

        private readonly object _lock = new object();
        private int _last;

        public T Lease()
        {
            return Lease(_connections);
        }

        public T Lease(List<T> connections)
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
