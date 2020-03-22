using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hummingbird.Extersions.EventBus.Kafka
{
    public interface IRabbitMQPersisterConnectionLoadBalancer
    {
        Task<IKafkaPersistentConnection> Lease();

    }
}