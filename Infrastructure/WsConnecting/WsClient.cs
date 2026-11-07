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
                    await _socket.CloseAsync(
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
            await _socket.SendAsync(bytes, WebSocketMessageType.Text, true, _cts.Token);
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[4096];

            try
            {
                while (_socket.State == WebSocketState.Open)
                {
                    var result = await _socket.ReceiveAsync(buffer, _cts.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);

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
            var doc = JsonDocument.Parse(json);

            // todo: use model?
            var type = doc.RootElement.GetProperty("type").GetString();
            var requestId = doc.RootElement.GetProperty("requestId").GetString();
            var payload = doc.RootElement.GetProperty("payload");

            var result = await _handlerRegistry.Invoke(type, payload);

            var response = new
            {
                type = type + "Response",
                requestId,
                payload = result
            };

            var responseJson = JsonSerializer.Serialize(response);

            await SendAsync(responseJson);
        }
    }
}
