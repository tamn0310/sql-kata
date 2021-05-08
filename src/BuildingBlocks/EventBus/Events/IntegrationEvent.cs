using Newtonsoft.Json;
using System;

namespace EventBus.Events
{
    public class IntegrationEvent
    {
        public IntegrationEvent()
        {
            this.Id = Guid.NewGuid();
            this.CreationDate = DateTime.UtcNow;
        }

        [JsonConstructor]
        public IntegrationEvent(Guid id, DateTime createDate)
        {
            this.Id = id;
            this.CreationDate = createDate;
        }

        [JsonProperty]
        public Guid Id { get; private set; }

        [JsonProperty]
        public DateTime CreationDate { get; private set; }
    }
}