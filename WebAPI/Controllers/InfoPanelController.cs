using Application.Services;
using Domen.Models;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InfoPanelController : ControllerBase
    {
        private readonly ILogger<InfoPanelController> _logger;
        private readonly InfoPanelService _infoPanelService;

        public InfoPanelController(
            ILogger<InfoPanelController> logger,
            InfoPanelService infoPanelService
        )
        {
            _logger = logger;
            _infoPanelService = infoPanelService;
        }

        [HttpGet("get-route-panel")]
        public ActionResult<RoutePanel> GetRoutePanel()
        {
            var routePanel = _infoPanelService.GetRoutePanel();
            return Ok(routePanel);
        }

        [HttpGet("get-location")]
        public ActionResult<LocationInfo> GetLocation()
        {
            var location = _infoPanelService.GetLocation();
            return Ok(location);
        }
    }
}
