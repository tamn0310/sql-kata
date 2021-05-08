using Autofac;
using EventBus;
using EventBus.Abstractions;
using EventBus.Events;
using EventBus.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQ
{
    public class EventBusRabbitMQ : IEventBus, IDisposable
    {
        private const string BROKER_NAME = "jst_event_bus";

        private readonly IRabbitMQPersistentConnection _persistentConnection;
        private readonly ILogger<EventBusRabbitMQ> _logger;
        private readonly IEventBusSubscriptionsManager _subsManager;
        private readonly ILifetimeScope _autofac;
        private readonly string _aUTOFAC_SCOPE_NAME = "jst_event_bus";
        private readonly int _retryCount;

        private IModel _consumerChannel;
        private string _queueName;

        public EventBusRabbitMQ(IRabbitMQPersistentConnection persistentConnection,
            ILogger<EventBusRabbitMQ> logger,
            ILifetimeScope autofac,
            IEventBusSubscriptionsManager subsManager,
            string queueName = null,
            int retryCount = 5)
        {
            this._persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._subsManager = subsManager ?? new InMemoryEventBusSubscriptionsManager();
            this._queueName = queueName;
            this._consumerChannel = this.CreateConsumerChannel();
            this._autofac = autofac;
            this._retryCount = retryCount;
            this._subsManager.OnEventRemoved += this.SubsManager_OnEventRemoved;
        }

        private void SubsManager_OnEventRemoved(object sender, string eventName)
        {
            if (!this._persistentConnection.IsConnected)
            {
                this._persistentConnection.TryConnect();
            }

            using (var channel = this._persistentConnection.CreateModel())
            {
                channel.QueueUnbind(queue: this._queueName,
                    exchange: BROKER_NAME,
                    routingKey: eventName);

                if (this._subsManager.IsEmpty)
                {
                    this._queueName = string.Empty;
                    this._consumerChannel.Close();
                }
            }
        }

        public void Publish(IntegrationEvent @event)
        {
            if (!this._persistentConnection.IsConnected)
            {
                this._persistentConnection.TryConnect();
            }

            var policy = Policy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(this._retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    this._logger.LogWarning(ex, "Could not publish event: {EventId} after {Timeout}s ({ExceptionMessage})", @event.Id, $"{time.TotalSeconds:n1}", ex.Message);
                });

            var eventName = @event.GetType().Name;

            this._logger.LogTrace("Creating RabbitMQ channel to publish event: {EventId} ({EventName})", @event.Id, eventName);

            using var channel = this._persistentConnection.CreateModel();
            this._logger.LogTrace("Declaring RabbitMQ exchange to publish event: {EventId}", @event.Id);

            channel.ExchangeDeclare(exchange: BROKER_NAME, type: "direct");

            var message = JsonConvert.SerializeObject(@event);
            var body = Encoding.UTF8.GetBytes(message);

            policy.Execute(() =>
            {
                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = 2; // persistent

                    this._logger.LogTrace("Publishing event to RabbitMQ: {EventId}", @event.Id);

                channel.BasicPublish(
                    exchange: BROKER_NAME,
                    routingKey: eventName,
                    mandatory: true,
                    basicProperties: properties,
                    body: body);
            });
        }

        public void SubscribeDynamic<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler
        {
            this._logger.LogInformation("Subscribing to dynamic event {EventName} with {EventHandler}", eventName, typeof(TH).GetGenericTypeName());

            this.DoInternalSubscription(eventName);
            this._subsManager.AddDynamicSubscription<TH>(eventName);
            this.StartBasicConsume();
        }

        public void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = this._subsManager.GetEventKey<T>();
            this.DoInternalSubscription(eventName);

            this._logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName, typeof(TH).GetGenericTypeName());

            this._subsManager.AddSubscription<T, TH>();
            this.StartBasicConsume();
        }

        private void DoInternalSubscription(string eventName)
        {
            var containsKey = this._subsManager.HasSubscriptionsForEvent(eventName);
            if (!containsKey)
            {
                if (!this._persistentConnection.IsConnected)
                {
                    this._persistentConnection.TryConnect();
                }

                using (var channel = this._persistentConnection.CreateModel())
                {
                    channel.QueueBind(queue: this._queueName,
                                      exchange: BROKER_NAME,
                                      routingKey: eventName);
                }
            }
        }

        public void Unsubscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = this._subsManager.GetEventKey<T>();

            this._logger.LogInformation("Unsubscribing from event {EventName}", eventName);

            this._subsManager.RemoveSubscription<T, TH>();
        }

        public void UnsubscribeDynamic<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler
        {
            this._subsManager.RemoveDynamicSubscription<TH>(eventName);
        }

        public void Dispose()
        {
            if (this._consumerChannel != null)
            {
                this._consumerChannel.Dispose();
            }

            this._subsManager.Clear();
        }

        private void StartBasicConsume()
        {
            this._logger.LogTrace("Starting RabbitMQ basic consume");

            if (this._consumerChannel != null)
            {
                var consumer = new AsyncEventingBasicConsumer(this._consumerChannel);

                consumer.Received += this.Consumer_Received;

                this._consumerChannel.BasicConsume(
                    queue: this._queueName,
                    autoAck: false,
                    consumer: consumer);
            }
            else
            {
                this._logger.LogError("StartBasicConsume can't call on _consumerChannel == null");
            }
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
        {
            var eventName = eventArgs.RoutingKey;
            var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

            try
            {
                if (message.ToLowerInvariant().Contains("throw-fake-exception"))
                {
                    throw new InvalidOperationException($"Fake exception requested: \"{message}\"");
                }

                await this.ProcessEvent(eventName, message);
            }
            catch (Exception ex)
            {
                this._logger.LogWarning(ex, "----- ERROR Processing message \"{Message}\"", message);
            }

            // Even on exception we take the message off the queue.
            // in a REAL WORLD app this should be handled with a Dead Letter Exchange (DLX).
            // For more information see: https://www.rabbitmq.com/dlx.html
            this._consumerChannel.BasicAck(eventArgs.DeliveryTag, multiple: false);
        }

        private IModel CreateConsumerChannel()
        {
            if (!this._persistentConnection.IsConnected)
            {
                this._persistentConnection.TryConnect();
            }

            this._logger.LogTrace("Creating RabbitMQ consumer channel");

            var channel = this._persistentConnection.CreateModel();

            channel.ExchangeDeclare(exchange: BROKER_NAME,
                                    type: "direct");

            channel.QueueDeclare(queue: this._queueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            channel.CallbackException += (sender, ea) =>
            {
                this._logger.LogWarning(ea.Exception, "Recreating RabbitMQ consumer channel");

                this._consumerChannel.Dispose();
                this._consumerChannel = this.CreateConsumerChannel();
                this.StartBasicConsume();
            };

            return channel;
        }

        private async Task ProcessEvent(string eventName, string message)
        {
            this._logger.LogTrace("Processing RabbitMQ event: {EventName}", eventName);

            if (this._subsManager.HasSubscriptionsForEvent(eventName))
            {
                using var scope = this._autofac.BeginLifetimeScope(this._aUTOFAC_SCOPE_NAME);
                var subscriptions = this._subsManager.GetHandlersForEvent(eventName);
                foreach (var subscription in subscriptions)
                {
                    if (subscription.IsDynamic)
                    {
                        var handler = scope.ResolveOptional(subscription.HandlerType) as IDynamicIntegrationEventHandler;
                        if (handler == null) continue;

                        dynamic eventData = JObject.Parse(message);

                        await Task.Yield();
                        await handler.Handle(eventData);
                    }
                    else
                    {
                        var handler = scope.ResolveOptional(subscription.HandlerType);
                        if (handler == null) continue;
                        var eventType = this._subsManager.GetEventTypeByName(eventName);
                        var integrationEvent = JsonConvert.DeserializeObject(message, eventType);
                        var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);

                        await Task.Yield();
                        await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { integrationEvent });
                    }
                }
            }
            else
            {
                this._logger.LogWarning("No subscription for RabbitMQ event: {EventName}", eventName);
            }
        }
    }
}