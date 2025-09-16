using System.Net.WebSockets;
using System.Text;
using NLog;

namespace DotQuant.Ui.Services;

public class WebSocketClient : IDisposable
{
    private ClientWebSocket? _webSocket;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly CancellationTokenSource _cts = new();
    private readonly string _serverUrl;

    public WebSocketClient(IConfiguration configuration)
    {
        _serverUrl = configuration["ApiEndpoints:WebSocketUrl"];
    }

    public async Task ConnectAsync()
    {
        if (_webSocket != null && _webSocket.State == WebSocketState.Open)
        {
            Logger.Warn("WebSocket is already connected.");
            return;
        }

        try
        {
            _webSocket = new ClientWebSocket();
            Logger.Info("Connecting to WebSocket server at {0}", _serverUrl);
            await _webSocket.ConnectAsync(new Uri(_serverUrl), _cts.Token);
            Logger.Info("Connected to WebSocket server.");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to connect to WebSocket server: {ex.GetBaseException().Message}");
            Dispose(); // Ensure proper cleanup
        }
    }

    /// <summary>
    /// Begin receiving messages in a loop. Call <paramref name="messageHandler"/> for each message.
    /// This method typically would be awaited or run in a background task.
    /// </summary>
    public async Task ReceiveAsync(Action<string> messageHandler)
    {
        if (_webSocket == null)
        {
            Logger.Warn("WebSocket client is not connected.");
            return;
        }

        var buffer = new byte[1024];

        try
        {
            while (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(buffer, _cts.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Logger.Info("WebSocket connection closed by server.");
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Logger.Info("WebSocket message received: {0}", message);
                messageHandler.Invoke(message);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error receiving WebSocket messages: {ex.GetBaseException().Message}");
        }
    }

    /// <summary>
    /// Sends a subscription request to the server for the specified channel.
    /// </summary>
    public async Task SubscribeAsync(string channelName)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            Logger.Warn("Cannot subscribe. WebSocket is not open.");
            return;
        }

        var msg = $"SUBSCRIBE:{channelName}";
        var buffer = Encoding.UTF8.GetBytes(msg);
        await _webSocket.SendAsync(
            new ArraySegment<byte>(buffer),
            WebSocketMessageType.Text,
            endOfMessage: true,
            _cts.Token
        );
    }

    /// <summary>
    /// Sends an unsubscription request to the server for the specified channel.
    /// </summary>
    public async Task UnsubscribeAsync(string channelName)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            Logger.Warn("Cannot unsubscribe. WebSocket is not open.");
            return;
        }

        var msg = $"UNSUBSCRIBE:{channelName}";
        var buffer = Encoding.UTF8.GetBytes(msg);
        await _webSocket.SendAsync(
            new ArraySegment<byte>(buffer),
            WebSocketMessageType.Text,
            endOfMessage: true,
            _cts.Token
        );
    }

    /// <summary>
    /// Sends a generic message (for example, to broadcast to "global" or for some other server logic).
    /// </summary>
    public async Task SendMessageAsync(string message)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            Logger.Warn("Cannot send message. WebSocket is not open.");
            return;
        }

        var buffer = Encoding.UTF8.GetBytes(message);
        await _webSocket.SendAsync(
            new ArraySegment<byte>(buffer),
            WebSocketMessageType.Text,
            endOfMessage: true,
            _cts.Token
        );
    }

    public async Task DisconnectAsync()
    {
        if (_webSocket != null && _webSocket.State == WebSocketState.Open)
        {
            Logger.Info("Closing WebSocket connection...");
            await _webSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Closed by client",
                _cts.Token
            );
            Logger.Info("WebSocket connection closed.");
        }

        Dispose(); // Ensure cleanup
    }

    public void Dispose()
    {
        Logger.Info("Disposing WebSocket client...");
        _cts.Cancel();
        _webSocket?.Dispose();
        _webSocket = null;
    }
}