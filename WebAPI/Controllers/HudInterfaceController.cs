using Application.Services;
using Domen.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HudInterfaceController : ControllerBase
    {
        private readonly ILogger<HudInterfaceController> _logger;
        private readonly HudInterfaceService _hudInterfaceService;

        public HudInterfaceController(
            ILogger<HudInterfaceController> logger,
            HudInterfaceService hudInterfaceService
        )
        {
            _logger = logger;
            _hudInterfaceService = hudInterfaceService;
        }

        [HttpGet("get-hud-info")]
        public ActionResult<HudInterface> GetHudInfo()
        {
            var overViewInfo = _hudInterfaceService.GetInfo();
            return Ok(overViewInfo);
        }

        [HttpGet("get-ship-hp")]
        public ActionResult<HealthPoints> GetShipHP()
        {
            var overViewInfo = _hudInterfaceService.GetShipHP();
            return Ok(overViewInfo);
        }

        [HttpGet("get-current-speed")]
        public ActionResult<int> GetCurrentSpeed()
        {
            var overViewInfo = _hudInterfaceService.GetCurrentSpeed();
            return Ok(overViewInfo);
        }

        [HttpGet("get-all-modules")]
        public ActionResult<IEnumerable<ShipModule>> GetAllModules()
        {
            var overViewInfo = _hudInterfaceService.GetAllModules();
            return Ok(overViewInfo);
        }

        [HttpGet("get-ship-flight-mode")]
        public ActionResult<ShipFlightMode> GetShipFlightMode()
        {
            var overViewInfo = _hudInterfaceService.GetShipFlightMode();
            return Ok(overViewInfo);
        }

        [HttpGet("get-center-pos")]
        public ActionResult<Point> GetCenterPos()
        {
            var overViewInfo = _hudInterfaceService.GetCenterPos();
            return Ok(overViewInfo);
        }

        [HttpPost("ship-stop")]
        public IActionResult ShipStop()
        {
            _hudInterfaceService.ShipStop();

            return Ok(new { message = "ship stopped" });
        }

        [HttpPost("set-full-speed")]
        public IActionResult SetFullSpeed()
        {
            _hudInterfaceService.SetFullSpeed();

            return Ok(new { message = "full speed set" });
        }

        [HttpPost("toggle-activation-module")]
        public IActionResult ToggleActivationModule([FromBody] string moduleName)
        {
            _hudInterfaceService.ToggleActivationModule(moduleName);

            return Ok(new { message = "activation module toggled" });
        }
    }
}
