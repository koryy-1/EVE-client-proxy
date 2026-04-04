using Application.Services;
using Domen.Models;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DroneController : ControllerBase
    {
        private readonly ILogger<DroneController> _logger;
        private readonly DroneService _droneService;

        public DroneController(
            ILogger<DroneController> logger,
            DroneService droneService
        )
        {
            _logger = logger;
            _droneService = droneService;
        }

        [HttpGet("get-drones-info")]
        public ActionResult<IEnumerable<Drone>> GetDronesInfo()
        {
            var dronesInfo = _droneService.GetInfo();
            return Ok(dronesInfo);
        }

        [HttpPost("launch")]
        public IActionResult Launch()
        {
            _droneService.Launch();
            return Ok();
        }

        [HttpPost("engage")]
        public IActionResult Engage()
        {
            _droneService.Engage();
            return Ok();
        }

        [HttpPost("scoop")]
        public IActionResult Scoop()
        {
            _droneService.Scoop();
            return Ok();
        }

        //todo: make method launch / engage / return concrete drone
    }
}
