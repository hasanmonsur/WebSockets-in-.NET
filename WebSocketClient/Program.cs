// See https://aka.ms/new-console-template for more information
using System.Net.WebSockets;
using System.Text.Json;
using System.Text;
using WebSocketClient;

Console.WriteLine("Hello, World!");
var client = new ClientWebSocket();
var cancellationToken = new CancellationTokenSource();

Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cancellationToken.Cancel();
};

try
{
    await client.ConnectAsync(new Uri("ws://localhost:5084/ws"), cancellationToken.Token);
    Console.WriteLine("Connected to WebSocket server");

    // Start receiving messages
    var receiveTask = ReceiveMessagesAsync(client, cancellationToken.Token);

    // Send messages
    while (client.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
    {
        Console.Write("Enter message (or 'exit' to quit): ");
        var message = Console.ReadLine();

        if (message == "exit" || cancellationToken.IsCancellationRequested)
            break;

        var webSocketMessage = new WebSocketMessage
        {
            MessageType = "chat",
            Payload = message,
            Metadata = { ["client"] = "console-client" }
        };

        var jsonMessage = JsonSerializer.Serialize(webSocketMessage);
        var bytes = Encoding.UTF8.GetBytes(jsonMessage);
        await client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken.Token);
    }

    if (client.State == WebSocketState.Open)
    {
        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing", CancellationToken.None);
    }

    await receiveTask;
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
finally
{
    client.Dispose();
}

async Task ReceiveMessagesAsync(WebSocket socket, CancellationToken cancellationToken)
{
    var buffer = new byte[1024 * 4];

    try
    {
        while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                break;
            }

            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var webSocketMessage = JsonSerializer.Deserialize<WebSocketMessage>(message);

            Console.WriteLine($"Received: {webSocketMessage.Payload} (Type: {webSocketMessage.MessageType})");
        }
    }
    catch (OperationCanceledException)
    {
        // Normal shutdown
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Receive error: {ex.Message}");
    }
}