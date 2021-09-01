using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
namespace Hummingbird.Extensions.EventBus.Kafka
{
    public class DefaultKafkaPersistentConnection
       : IKafkaPersistentConnection
    {
        private static object _syncRoot = new object();
        private readonly ILogger<IKafkaPersistentConnection> _logger;
        private readonly ConsumerBuilder<string, string> _consumerBuilder;
        private readonly ProducerBuilder<string, string> _producerBuilder;
        private readonly List<IConsumer<string, string>> _consumers;
        private IProducer<string, string> _producer;
        private bool _disposed;

        public IProducer<string, string> GetProducer()
        {
            if (_producer == null)
            {
                lock (_syncRoot)
                {
                    if (_producer == null)
                    {
                        _producer = _producerBuilder.Build();
                    }
                }
            }
          
            return _producer;
        }

        public IConsumer<string, string> GetConsumer()
        {
            var customer= _consumerBuilder.Build();
            _consumers.Add(customer);
            return customer;
        }

        public DefaultKafkaPersistentConnection(
         ILogger<IKafkaPersistentConnection> logger,
         ProducerConfig producerConfig)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _producerBuilder = new ProducerBuilder<string, string>(producerConfig);
           
        }

        public DefaultKafkaPersistentConnection(
            ILogger<IKafkaPersistentConnection> logger,
            ConsumerConfig consumerConfig)
        {
            consumerConfig.EnableAutoOffsetStore = false;
            consumerConfig.EnableAutoCommit = true;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _consumers = new List<IConsumer<string, string>>();
            _consumerBuilder = new ConsumerBuilder<string, string>(consumerConfig);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            try
            {
                if (_consumers != null)
                {
                    _consumers.ForEach(customer =>
                    {
                        customer.Dispose();
                    });
                }

                if (_producer != null)
                {
                   
                    _producer.Dispose();
                }
            }
            catch (IOException ex)
            {
                _logger.LogCritical(ex.ToString());
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}
