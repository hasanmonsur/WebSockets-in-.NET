namespace WebSocketServer
{
    public class WebSocketMessage
    {
        public string MessageType { get; set; }
        public string Payload { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
