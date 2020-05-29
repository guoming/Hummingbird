using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hummingbird.Extensions.EventBus.Kafka
{
    public interface IRabbitMQPersisterConnectionLoadBalancer
    {
        Task<IKafkaPersistentConnection> Lease();

    }
}