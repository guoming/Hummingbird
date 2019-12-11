using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.LoadBalancers
{
    internal class RandomRobinLoadBalancer<T> : ILoadBalancer<T>
    {
        private readonly Func<List<T>> _func;
        public RandomRobinLoadBalancer(Func<List<T>> func)
        {
            this._func = func;
        }

        private readonly object _lock = new object();

        public T Lease()
        {            
            var connection = _func();
            int _last = new Random(Guid.NewGuid().GetHashCode()).Next(connection.Count-1);
            lock (_lock)
            {
                if (_last < connection.Count)
                {
                    _last = 0;
                }

                if (_last > connection.Count)
                {
                    _last = 0;
                }

                var next = connection[_last];

                return next;
            }
        }
    }
}
