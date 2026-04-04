using Application.Services;
using Domen.dto;
using Domen.Models;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SelectItemController : ControllerBase
    {
        private readonly ILogger<SelectItemController> _logger;
        private readonly SelectItemService _selectItemService;

        public SelectItemController(
            ILogger<SelectItemController> logger,
            SelectItemService selectItemService
        )
        {
            _logger = logger;
            _selectItemService = selectItemService;
        }

        [HttpGet("get-select-item-info")]
        public ActionResult<SelectedItemInfo> GetSelectItemInfo()
        {
            var routePanel = _selectItemService.GetInfo();
            return Ok(routePanel);
        }

        [HttpPost("click-button")]
        public IActionResult ClickButton([FromBody] string btnName)
        {
            var success = _selectItemService.ClickButton(btnName);
            if (success)
                return Ok(new { message = "Button found" });

            return Ok(new { message = "Button not found" });
            //return BadRequest("Button not found");
        }
    }
}
