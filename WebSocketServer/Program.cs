using Microsoft.OpenApi.Models;
using WebSocketServer;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddSingleton<IWebSocketService, WebSocketManagerService>();




builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
});


// WebSocket endpoint
app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var webSocketService = context.RequestServices.GetRequiredService<IWebSocketService>();

        await webSocketService.HandleConnectionAsync(webSocket);
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
    }
});

app.Run();