using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

namespace Hummingbird.Extersions.EventBus.RabbitMQ
{
    public class DefaultRabbitMQPersistentConnection
       : IRabbitMQPersistentConnection
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger<IRabbitMQPersistentConnection> _logger;
        private readonly int _retryCount;
        private bool _disposed = false;
        private object sync_root = new object();
        private IConnection _connection;
        private IModel _producer;
        private List<IModel> _consumers;

        public DefaultRabbitMQPersistentConnection(IConnectionFactory connectionFactory, ILogger<IRabbitMQPersistentConnection> logger, int retryCount = 5)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _retryCount = retryCount;
            _consumers = new List<IModel>();
        }

        public bool IsConnected
        {
            get
            {
                return _connection != null && _connection.IsOpen && !_disposed;
            }
        }

        public IModel GetConsumer()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
            }


            var consumer= _connection.CreateModel();
            _consumers.Add(consumer);
            return consumer;
         
        }

        public IModel GetProducer()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
            }

            if (_producer == null)
            {
                lock (sync_root)
                {
                    if (_producer == null)
                    {
                        _producer = GetConsumer();
                        _producer.ConfirmSelect();
                    }
                }
            }

            return _producer;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                if(_consumers!=null)
                {
                    _consumers.ForEach(consumer =>
                    {
                        consumer.Close();
                        consumer.Dispose();

                    });
                }

                if(_producer!=null)
                {
                    _producer.Close();
                    _producer.Dispose();
                }

                if (_connection != null)
                {
                    _connection.Close();
                    _connection.Dispose();
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

        public bool TryConnect()
        {
            _logger.LogInformation("RabbitMQ Client is trying to connect");

            if (!IsConnected)
            {
                lock (sync_root)
                {
                    if (!IsConnected)
                    {
                        var policy = RetryPolicy.Handle<SocketException>()
                            .Or<BrokerUnreachableException>()
                            .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                            {
                                _logger.LogWarning(ex.ToString());
                            }
                        );

                        policy.Execute(() =>
                        {
                            _connection = _connectionFactory.CreateConnection();                            
                        });

                        if (IsConnected)
                        {
                            _connection.ConnectionShutdown += OnConnectionShutdown;
                            _connection.CallbackException += OnCallbackException;
                            _connection.ConnectionBlocked += OnConnectionBlocked;
                            return true;
                        }
                        else
                        {
                            _logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");

                            return false;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            else
            {
                return true;
            }
        }

        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect...");

            TryConnect();
        }

        void OnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");

            TryConnect();
        }

        void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");

            TryConnect();
        }
    }
}
