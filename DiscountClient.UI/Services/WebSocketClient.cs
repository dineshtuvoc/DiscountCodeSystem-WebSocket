using DiscountClient.UI.Models;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiscountClient.UI.Services;

public class WebSocketClient
{
    private readonly ClientWebSocket _ws = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly JsonSerializerOptions _jsonOptions;

    // Event to send received messages back to the UI
    public event Action<string>? MessageReceived;

    public WebSocketClient()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }

    public async Task ConnectAsync(string uri)
    {
        if (_ws.State == WebSocketState.Open) return;

        await _ws.ConnectAsync(new Uri(uri), _cts.Token);
        // Start listening for messages in the background
        _ = Task.Run(ReceiveLoop);
    }

    public async Task DisconnectAsync()
    {
        if (_ws.State == WebSocketState.Open)
        {
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", _cts.Token);
        }
        _cts.Cancel();
    }

    public async Task SendMessageAsync<T>(MessageType type, T payload)
    {
        if (_ws.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket is not connected.");
        }

        var message = new BaseMessage
        {
            Type = type,
            Payload = JsonSerializer.Serialize(payload, _jsonOptions)
        };
        var jsonMessage = JsonSerializer.Serialize(message, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(jsonMessage);
        await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
    }

    private async Task ReceiveLoop()
    {
        var buffer = new byte[1024 * 4];
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await DisconnectAsync();
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                // Raise the event to notify the UI
                MessageReceived?.Invoke(message);
            }
            catch (WebSocketException)
            {
                // Connection closed unexpectedly
                MessageReceived?.Invoke("Connection to server lost.");
                break;
            }
        }
    }
}