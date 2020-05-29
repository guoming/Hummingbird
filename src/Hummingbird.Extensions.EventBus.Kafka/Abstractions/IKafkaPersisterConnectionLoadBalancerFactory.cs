using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hummingbird.Extensions.EventBus.Kafka
{
    public interface IKafkaPersisterConnectionLoadBalancerFactory
    {
        IRabbitMQPersisterConnectionLoadBalancer Get(Func<List<IKafkaPersistentConnection>> func,string Type);
    }
}