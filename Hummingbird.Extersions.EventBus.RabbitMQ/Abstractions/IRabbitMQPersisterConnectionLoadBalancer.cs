using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hummingbird.Extersions.EventBus.RabbitMQ
{
    public interface IRabbitMQPersisterConnectionLoadBalancer
    {
        Task<IRabbitMQPersistentConnection> Lease();

    }
}