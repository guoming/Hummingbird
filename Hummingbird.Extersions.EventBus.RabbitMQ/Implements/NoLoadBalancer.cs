using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Extersions.EventBus.RabbitMQ.LoadBalancers
{
    public class NoLoadBalancer : IRabbitMQPersisterConnectionLoadBalancer
    {
        private readonly Func<List<IRabbitMQPersistentConnection>> _func;
        public NoLoadBalancer(Func<List<IRabbitMQPersistentConnection>> func)
        {
            this._func = func;

        }


        public async Task<IRabbitMQPersistentConnection> Lease()
        {
            var connections = _func();

            if (connections == null || connections.Count == 0)
            {
                throw new Exception("There were no connections in NoLoadBalancer");
            }

            var connection = await Task.FromResult(connections.FirstOrDefault());
            return connection;
        }
    }
}
