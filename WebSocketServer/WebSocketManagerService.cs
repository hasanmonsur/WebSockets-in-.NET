using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace WebSocketServer
{
    public class WebSocketManagerService : IWebSocketService
    {
        private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();
        private readonly ILogger<WebSocketManagerService> _logger;

        public WebSocketManagerService(ILogger<WebSocketManagerService> logger)
        {
            _logger = logger;
        }

        public async Task HandleConnectionAsync(WebSocket webSocket)
        {
            var connectionId = Guid.NewGuid().ToString();
            _sockets.TryAdd(connectionId, webSocket);

            try
            {
                var buffer = new byte[1024 * 4];
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                while (!result.CloseStatus.HasValue)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logger.LogInformation($"Received message from {connectionId}: {message}");

                    // Process message
                    var response = new WebSocketMessage
                    {
                        MessageType = "echo",
                        Payload = $"Server received: {message}",
                        Metadata = { ["sender"] = connectionId }
                    };

                    await SendMessageAsync(webSocket, JsonSerializer.Serialize(response));

                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }

                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"WebSocket connection error for {connectionId}");
            }
            finally
            {
                _sockets.TryRemove(connectionId, out _);
                webSocket.Dispose();
            }
        }

        public async Task SendMessageAsync(string message)
        {
            foreach (var socket in _sockets.Values.Where(s => s.State == WebSocketState.Open))
            {
                await SendMessageAsync(socket, message);
            }
        }

        public async Task BroadcastAsync(string message)
        {
            var tasks = _sockets.Values
                .Where(s => s.State == WebSocketState.Open)
                .Select(socket => SendMessageAsync(socket, message));

            await Task.WhenAll(tasks);
        }

        public async Task DisconnectAllAsync()
        {
            foreach (var (id, socket) in _sockets)
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure,
                        "Server shutting down", CancellationToken.None);
                }
                socket.Dispose();
            }
            _sockets.Clear();
        }

        public int GetActiveConnectionsCount() => _sockets.Count;

        private async Task SendMessageAsync(WebSocket socket, string message)
        {
            if (socket.State != WebSocketState.Open) return;

            var bytes = Encoding.UTF8.GetBytes(message);
            await socket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length),
                WebSocketMessageType.Text, true, CancellationToken.None);
        }

       
    }
}
