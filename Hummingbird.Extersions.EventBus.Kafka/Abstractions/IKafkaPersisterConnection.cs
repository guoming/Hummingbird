using Confluent.Kafka;
using System;

namespace Hummingbird.Extersions.EventBus.Kafka
{
    public interface IKafkaPersistentConnection
        : IDisposable
    {

        IProducer<string, string> GetProducer();

         IConsumer<string, string> GetConsumer();
    }
}
