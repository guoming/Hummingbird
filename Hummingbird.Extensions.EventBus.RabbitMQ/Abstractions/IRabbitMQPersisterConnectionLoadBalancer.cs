using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hummingbird.Extensions.EventBus.RabbitMQ
{
    public interface IRabbitMQPersisterConnectionLoadBalancer
    {
        Task<IRabbitMQPersistentConnection> Lease();

    }
}