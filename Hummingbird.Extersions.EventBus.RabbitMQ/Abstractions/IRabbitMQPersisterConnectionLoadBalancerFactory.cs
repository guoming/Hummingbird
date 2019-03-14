using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hummingbird.Extersions.EventBus.RabbitMQ
{
    public interface IRabbitMQPersisterConnectionLoadBalancerFactory
    {
        IRabbitMQPersisterConnectionLoadBalancer Get(Func<List<IRabbitMQPersistentConnection>> func,string Type);
    }
}