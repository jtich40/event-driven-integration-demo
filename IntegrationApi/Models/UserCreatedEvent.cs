namespace IntegrationApi.Models
{
    public class UserCreatedEvent
    {
        public string EventId { get; set; } = string.Empty;
        public string EventType { get; set; } = "UserCreated";
        public DateTime Timestamp { get; set; }
        public UserEventData User { get; set; } = new UserEventData();
    }

    public class UserEventData
    {
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;    
    }
}