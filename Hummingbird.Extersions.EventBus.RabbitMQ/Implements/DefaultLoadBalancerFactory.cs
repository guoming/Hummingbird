using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Extersions.EventBus.RabbitMQ.LoadBalancers
{
    public class DefaultLoadBalancerFactory : IRabbitMQPersisterConnectionLoadBalancerFactory
    {

        public IRabbitMQPersisterConnectionLoadBalancer Get(Func<List<IRabbitMQPersistentConnection>> func,string Type)
        {
            switch (Type)
            {
                case nameof(RoundRobinLoadBalancer):
                    return new RoundRobinLoadBalancer(func);
                default:
                    return new NoLoadBalancer(func);
            }
        }
    }
}
