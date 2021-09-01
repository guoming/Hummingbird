using Confluent.Kafka;
using System;

namespace Hummingbird.Extensions.EventBus.Kafka
{
    public interface IKafkaPersistentConnection
        : IDisposable
    {

        IProducer<string, string> GetProducer();

         IConsumer<string, string> GetConsumer();

    }
}
