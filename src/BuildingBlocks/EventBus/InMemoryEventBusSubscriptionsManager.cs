using EventBus.Abstractions;
using EventBus.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EventBus
{
    public partial class InMemoryEventBusSubscriptionsManager : IEventBusSubscriptionsManager
    {
        private readonly Dictionary<string, List<SubscriptionInfo>> _handlers;
        private readonly List<Type> _eventTypes;

        public event EventHandler<string> OnEventRemoved;

        public InMemoryEventBusSubscriptionsManager()
        {
            this._handlers = new Dictionary<string, List<SubscriptionInfo>>();
            this._eventTypes = new List<Type>();
        }

        public bool IsEmpty => !this._handlers.Keys.Any();
        public void Clear() => this._handlers.Clear();

        public void AddDynamicSubscription<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler
        {
            this.DoAddSubscription(typeof(TH), eventName, isDynamic: true);
        }

        public void AddSubscription<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = this.GetEventKey<T>();

            this.DoAddSubscription(typeof(TH), eventName, isDynamic: false);

            if (!this._eventTypes.Contains(typeof(T)))
            {
                this._eventTypes.Add(typeof(T));
            }
        }

        private void DoAddSubscription(Type handlerType, string eventName, bool isDynamic)
        {
            if (!this.HasSubscriptionsForEvent(eventName))
            {
                this._handlers.Add(eventName, new List<SubscriptionInfo>());
            }

            if (this._handlers[eventName].Any(s => s.HandlerType == handlerType))
            {
                throw new ArgumentException(
                    $"Handler Type {handlerType.Name} already registered for '{eventName}'", nameof(handlerType));
            }

            if (isDynamic)
            {
                this._handlers[eventName].Add(SubscriptionInfo.Dynamic(handlerType));
            }
            else
            {
                this._handlers[eventName].Add(SubscriptionInfo.Typed(handlerType));
            }
        }

        public void RemoveDynamicSubscription<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler
        {
            var handlerToRemove = this.FindDynamicSubscriptionToRemove<TH>(eventName);
            this.DoRemoveHandler(eventName, handlerToRemove);
        }

        public void RemoveSubscription<T, TH>()
            where TH : IIntegrationEventHandler<T>
            where T : IntegrationEvent
        {
            var handlerToRemove = this.FindSubscriptionToRemove<T, TH>();
            var eventName = this.GetEventKey<T>();
            this.DoRemoveHandler(eventName, handlerToRemove);
        }

        private void DoRemoveHandler(string eventName, SubscriptionInfo subsToRemove)
        {
            if (subsToRemove != null)
            {
                this._handlers[eventName].Remove(subsToRemove);
                if (!this._handlers[eventName].Any())
                {
                    this._handlers.Remove(eventName);
                    var eventType = this._eventTypes.SingleOrDefault(e => e.Name == eventName);
                    if (eventType != null)
                    {
                        this._eventTypes.Remove(eventType);
                    }

                    this.RaiseOnEventRemoved(eventName);
                }

            }
        }

        public IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IntegrationEvent
        {
            var key = this.GetEventKey<T>();
            return this.GetHandlersForEvent(key);
        }
        public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName) => this._handlers[eventName];

        private void RaiseOnEventRemoved(string eventName)
        {
            var handler = OnEventRemoved;
            handler?.Invoke(this, eventName);
        }


        private SubscriptionInfo FindDynamicSubscriptionToRemove<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler
        {
            return this.DoFindSubscriptionToRemove(eventName, typeof(TH));
        }


        private SubscriptionInfo FindSubscriptionToRemove<T, TH>()
             where T : IntegrationEvent
             where TH : IIntegrationEventHandler<T>
        {
            var eventName = this.GetEventKey<T>();
            return this.DoFindSubscriptionToRemove(eventName, typeof(TH));
        }

        private SubscriptionInfo DoFindSubscriptionToRemove(string eventName, Type handlerType)
        {
            if (!this.HasSubscriptionsForEvent(eventName))
            {
                return null;
            }

            return this._handlers[eventName].SingleOrDefault(s => s.HandlerType == handlerType);

        }

        public bool HasSubscriptionsForEvent<T>() where T : IntegrationEvent
        {
            var key = this.GetEventKey<T>();
            return this.HasSubscriptionsForEvent(key);
        }
        public bool HasSubscriptionsForEvent(string eventName) => this._handlers.ContainsKey(eventName);

        public Type GetEventTypeByName(string eventName) => this._eventTypes.SingleOrDefault(t => t.Name == eventName);

        public string GetEventKey<T>()
        {
            return typeof(T).Name;
        }
    }
}