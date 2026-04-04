using System.Threading.Tasks;
using Infrastructure.WsConnecting;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BotConnectionController: ControllerBase
    {
        private readonly ILogger<BotConnectionController> _logger;
        private readonly WsClient _wsClient;
        public BotConnectionController(
            ILogger<BotConnectionController> logger,
            WsClient wsClient
        )
        {
            _logger = logger;
            _wsClient = wsClient;
        }

        [HttpGet("check-bot-connection")]
        public ActionResult<bool> CheckBotConnection()
        {
            var isConnected = _wsClient.IsConnected();
            return Ok(isConnected);
        }

        [HttpPost("connect-to-bot")]
        public async Task<ActionResult<bool>> ConnectToBot([FromBody] string url)
        {
            await _wsClient.ConnectAsync(url);
            return Ok(true);
        }

        [HttpPost("disconnect-from-bot")]
        public async Task<ActionResult<bool>> DisconnectFromBot()
        {
            await _wsClient.DisconnectAsync();
            return Ok(true);
        }
    }
}
