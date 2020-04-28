using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hummingbird.Extensions.EventBus.RabbitMQ
{
    public interface IRabbitMQPersisterConnectionLoadBalancerFactory
    {
        IRabbitMQPersisterConnectionLoadBalancer Get(Func<List<IRabbitMQPersistentConnection>> func,string Type);
    }
}