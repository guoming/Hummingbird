using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Extersions.Cacheing.LoadBalancers
{
    internal class NoLoadBalancer : IConnectionLoadBalancer
    {
        private readonly List<StackExchangeImplement.RedisClientHelper> connections;
        public NoLoadBalancer(Func<List<StackExchangeImplement.RedisClientHelper>> func)
        {
            this.connections = func();

        }


        public StackExchangeImplement.RedisClientHelper Lease()
        {

            if (connections == null || connections.Count == 0)
            {
                throw new Exception("There were no connections in NoLoadBalancer");
            }

            var connection =connections.FirstOrDefault();
            return connection;
        }
    }
}
