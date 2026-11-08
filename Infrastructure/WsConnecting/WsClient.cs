using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Application.Services;

namespace Infrastructure.WsConnecting
{
    public class WsClient
    {
        private ClientWebSocket _socket;
        private CancellationTokenSource _cts;
        private readonly HandlerRegistry _handlerRegistry;
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public WsClient(HandlerRegistry handlerRegistry)
        {
            _socket = new();
            _cts = new();
            _handlerRegistry = handlerRegistry;
        }

        public bool IsConnected()
        {
            return _socket?.State == WebSocketState.Open;
        }

        public async Task ConnectAsync(string url)
        {
            _socket?.Dispose();

            _socket = new ClientWebSocket();
            _cts = new CancellationTokenSource();

            await _socket.ConnectAsync(new Uri(url), _cts.Token);
            _ = Task.Run(ReceiveLoop);
        }

        public async Task DisconnectAsync()
        {
            try
            {
                _cts.Cancel();

                if (_socket.State == WebSocketState.Open)
                {
                    await _socket.CloseOutputAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Disconnect",
                        CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                // todo: use default logger like in WebApi
                Console.WriteLine($"[WS ERROR] {ex.GetType().Name}: {ex.Message}");
            }
        }

        public async Task SendAsync(string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await _sendLock.WaitAsync(_cts.Token);
            try
            {
                await _socket.SendAsync(bytes, WebSocketMessageType.Text, true, _cts.Token);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[4096];

            try
            {
                while (_socket.State == WebSocketState.Open)
                {
                    using var messageBuffer = new MemoryStream();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _socket.ReceiveAsync(buffer, _cts.Token);
                        if (result.MessageType == WebSocketMessageType.Close)
                            break;

                        messageBuffer.Write(buffer, 0, result.Count);
                    }
                    while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    if (result.MessageType != WebSocketMessageType.Text)
                        continue;

                    var json = Encoding.UTF8.GetString(messageBuffer.ToArray());

                    await HandleMessage(json);
                }
            }
            catch (WebSocketException ex)
            {
                Console.WriteLine($"[WS ERROR] {ex.Message}");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[WS] Cancelled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WS UNKNOWN] {ex}");
            }

            Console.WriteLine("[WS] ReceiveLoop stopped");
        }

        private async Task HandleMessage(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var type = root.GetProperty("type").GetString();
            var requestId = root.GetProperty("requestId").GetString();
            var payload = root.TryGetProperty("payload", out var requestPayload)
                ? requestPayload
                : JsonDocument.Parse("null").RootElement;

            object result = null;
            object error = null;
            try
            {
                result = await _handlerRegistry.Invoke(type, payload);
            }
            catch (Exception ex)
            {
                error = new { code = "handler_error", message = ex.Message };
            }

            var response = new
            {
                type,
                requestId,
                payload = result,
                error
            };

            var responseJson = JsonSerializer.Serialize(response, JsonOptions);

            await SendAsync(responseJson);
        }
    }
}
