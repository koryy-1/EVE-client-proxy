using Application.Services;
using Domen.Models;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly ILogger<InventoryController> _logger;
        private readonly InventoryService _inventoryService;

        public InventoryController(
            ILogger<InventoryController> logger,
            InventoryService inventoryService
        )
        {
            _logger = logger;
            _inventoryService = inventoryService;
        }

        [HttpGet("get-inventory-info")]
        public ActionResult<IEnumerable<InventoryItem>> GetInventoryInfo()
        {
            var inventoryItems = _inventoryService.GetInfo();
            return Ok(inventoryItems);
        }

        [HttpGet("is-container-opened")]
        public ActionResult<bool> IsContainerOpened()
        {
            var isOpened = _inventoryService.IsContainerOpened();
            return Ok(isOpened);
        }

        [HttpPost("loot-all")]
        public IActionResult LootAll()
        {
            _inventoryService.LootAll();
            return Ok();
        }
    }
}
