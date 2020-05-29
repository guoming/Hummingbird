using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.LoadBalancers
{
    internal class RoundRobinLoadBalancer<T> : ILoadBalancer<T>
    {
        private readonly Func<List<T>> _func;
        public RoundRobinLoadBalancer(Func<List<T>> func)
        {
            this._func = func;

        }

        private readonly object _lock = new object();
        private int _last;

        public T Lease()
        {
            var connection = _func();
            return Lease(connection);
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
