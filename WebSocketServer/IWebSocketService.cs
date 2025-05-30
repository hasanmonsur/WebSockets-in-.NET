using System.Net.WebSockets;

namespace WebSocketServer
{
    public interface IWebSocketService
    {
        Task SendMessageAsync(string message);
        Task BroadcastAsync(string message);
        Task DisconnectAllAsync();
        int GetActiveConnectionsCount();
        Task HandleConnectionAsync(WebSocket webSocket);
    }
}
