using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hummingbird.LoadBalancers
{
    internal class NoLoadBalancer<T> : ILoadBalancer<T>
    {
        private readonly Func<List<T>> _func;
        private readonly List<T> _connections;

        public NoLoadBalancer(Func<List<T>> func)
        {
            this._func = func;
            this._connections = _func();
        }


        public T Lease()
        {
            return Lease(_connections);
        }

        public T Lease(List<T> connections)
        {

            if (connections == null || connections.Count == 0)
            {
                throw new Exception("There were no connections in NoLoadBalancer");
            }

            var connection = connections.FirstOrDefault();
            return connection;
        }
    }
}
