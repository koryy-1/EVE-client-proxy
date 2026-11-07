using Application.ClientWindow;
using Domen.dto;
using Microsoft.AspNetCore.Mvc;
using Domen.Models;
using System.Threading;
using Domen.Enums;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameClientController : ControllerBase
    {
        private readonly ILogger<GameClientController> _logger;
        private readonly GameClient _client;

        public GameClientController(
            ILogger<GameClientController> logger,
            GameClient client
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

        [HttpGet("get-searching-status")]
        public ActionResult<SearchingStatus> GetSearchingStatus()
        {
            _logger.LogDebug($"SearchingStatus = {_client.SearchingStatus}");
            return Ok(_client.SearchingStatus);
        }

        [HttpGet("get-search-task-status")]
        public ActionResult<SearchingStatus> GetSearchTaskStatus()
        {
            var searchTaskStatus = _client.GetSearchTaskStatus();
            _logger.LogDebug($"SearchTaskStatus = {searchTaskStatus}");
            return Ok(searchTaskStatus);
        }

        [HttpGet("check-root-address-actuality")]
        public ActionResult<SearchingStatus> CheckRootAddressActuality()
        {
            var isActualRootAddress = _client.CheckRootAddressActuality();
            _logger.LogDebug($"isActualRootAddress = {isActualRootAddress}");
            return Ok(isActualRootAddress);
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
