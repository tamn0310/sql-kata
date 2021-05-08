using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.IO;
using System.Net.Sockets;

namespace RabbitMQ
{
    public class DefaultRabbitMQPersistentConnection : IRabbitMQPersistentConnection
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger<DefaultRabbitMQPersistentConnection> _logger;
        private readonly int _retryCount;
        private IConnection _connection;
        private bool _disposed;
        private readonly object _sync_root = new object();

        public DefaultRabbitMQPersistentConnection(IConnectionFactory connectionFactory, ILogger<DefaultRabbitMQPersistentConnection> logger,
            int retryCount = 5)
        {
            this._connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._retryCount = retryCount;
        }

        public bool IsConnected => this._connection != null && this._connection.IsOpen && !this._disposed;

        public IModel CreateModel()
        {
            if (!this.IsConnected)
            {
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
            }

            return this._connection.CreateModel();
        }

        public void Dispose()
        {
            if (this._disposed) return;

            this._disposed = true;

            try
            {
                this._connection.Dispose();
            }
            catch (IOException ex)
            {
                this._logger.LogCritical(ex.ToString());
            }
        }

        public bool TryConnect()
        {
            this._logger.LogInformation("RabbitMQ Client is trying to connect");

            lock (this._sync_root)
            {
                var policy = Policy.Handle<SocketException>()
                    .Or<BrokerUnreachableException>()
                    .WaitAndRetry(this._retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                    {
                        this._logger.LogWarning(ex, "RabbitMQ Client could not connect after {TimeOut}s ({ExceptionMessage})", $"{time.TotalSeconds:n1}", ex.Message);
                    }
                );

                policy.Execute(() =>
                {
                    this._connection = this._connectionFactory
                          .CreateConnection();
                });

                if (this.IsConnected)
                {
                    this._connection.ConnectionShutdown += this.OnConnectionShutdown;
                    this._connection.CallbackException += this.OnCallbackException;
                    this._connection.ConnectionBlocked += this.OnConnectionBlocked;

                    this._logger.LogInformation("RabbitMQ Client acquired a persistent connection to '{HostName}' and is subscribed to failure events", this._connection.Endpoint.HostName);

                    return true;
                }
                else
                {
                    this._logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");

                    return false;
                }
            }
        }

        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (this._disposed) return;

            this._logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect...");

            this.TryConnect();
        }

        private void OnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (this._disposed) return;

            this._logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");

            this.TryConnect();
        }

        private void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
        {
            if (this._disposed) return;

            this._logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");

            this.TryConnect();
        }
    }
}