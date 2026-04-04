using Application.ClientWindow;
using Domen.dto;
using Microsoft.AspNetCore.Mvc;
using Domen.Models;
using System.Threading;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameClientController : ControllerBase
    {
        private readonly ILogger<GameClientController> _logger;
        private readonly Client _client;

        // todo: когда игра выключена вылезает failed to find by hwnd и приходится все равно перезапускать проект
        public GameClientController(
            ILogger<GameClientController> logger,
            Client client
        )
        {
            _logger = logger;
            _client = client;
        }

        [HttpPost("check-eve-client-open")]
        public ActionResult<ClientParamsDto> IsEveClientOpen([FromBody] string nickname)
        {
            var isOpen = _client.IsEveClientOpen(nickname);
            return Ok(isOpen);
        }

        [HttpPost("set-nickname")]
        public ActionResult<ClientParamsDto> SetNickname([FromBody] string nickname)
        {
            _client.SetNickname(nickname);
            return Ok(new { message = "Set nickname successfully" });
        }

        [HttpGet("get-client-params")]
        public ActionResult<ClientParamsDto> GetClientParams()
        {
            //todo: mapping
            return Ok(_client.ClientParams2Dto(_client.ClientParams));
        }

        [HttpGet("get-status")]
        public ActionResult<SearchingStatus> GetStatus()
        {
            _logger.LogDebug($"isActualRootAddress = {_client.Status.IsActualRootAddress}");
            return Ok(_client.Status);
        }

        [HttpGet("check-root-address-actuality")]
        public ActionResult<SearchingStatus> CheckRootAddressActuality()
        {
            var searchingStatus = _client.CheckRootAddressActuality();
            _logger.LogDebug($"isActualRootAddress = {searchingStatus.IsActualRootAddress}");
            return Ok(searchingStatus);
        }

        [HttpPost("update-client-params")]
        public IActionResult UpdateClientParams([FromBody] ClientParamsDto clientParamsDto)
        {
            var clientParams = _client.Dto2ClientParams(clientParamsDto);

            var success = _client.SetClientParams(clientParams);
            if (success)
            {
                _logger.LogDebug("ClientParams updated successfully");
                return Ok(new { message = "ClientParams updated successfully" });
            }
            else
            {
                _logger.LogDebug("Failed to update ClientParams");
                return BadRequest(new { message = "Failed to update ClientParams" });
            }
        }

        [HttpPost("start-search")]
        public ActionResult<ClientParamsDto> StartSearch()
        {
            if (_client.IsActualRootAddress(_client.ClientParams))
            {
                return Ok(_client.ClientParams2Dto(_client.ClientParams));
            }

            _logger.LogDebug("Search for root address started");

            //async func via Task.run
            _client.StartSearchingRootAddress();

            _logger.LogDebug($"The root address search is over, new root address is {_client.ClientParams.RootAddress}");

            return Ok(_client.ClientParams2Dto(_client.ClientParams));
        }

        [HttpPost("stopsearch")]
        public IActionResult StopSearch()
        {
            _client.StopSearching();
            return Ok(new { message = "root address search has been stopped" });
        }

        //todo: обработка ошибки когда клиент игры закрылся и рут адрес нужно заново искать
        //значит IsActualRootAddress = false;
    }
}
