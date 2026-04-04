using Application.ClientWindow;
using Application.Services;
using Domen.dto;
using Domen.Models;
using Microsoft.AspNetCore.Mvc;
using NLog.Targets;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OverViewController : ControllerBase
    {
        private readonly ILogger<OverViewController> _logger;
        private readonly OverViewService _overViewService;

        public OverViewController(
            ILogger<OverViewController> logger,
            OverViewService overViewService
        )
        {
            _logger = logger;
            _overViewService = overViewService;
        }

        [HttpGet("get-overview-info")]
        public ActionResult<IEnumerable<OverviewItem>> GetOverViewInfo()
        {
            var overViewInfo = _overViewService.GetInfo();
            return Ok(overViewInfo);
        }

        [HttpPost("click-on-object")]
        public IActionResult ClickOnObject([FromBody] OverviewItem spaceObject)
        {
            _overViewService.ClickOnObject(spaceObject);

            return Ok(new { message = "space object clicked" });
        }

        [HttpPost("lock-targets")]
        public IActionResult LockTargets([FromBody] IEnumerable<OverviewItem> targets)
        {
            _overViewService.LockTargets(targets);

            return Ok(new { message = "targets locked" });
        }

        [HttpPost("lock-target-by-name")]
        public IActionResult LockTargetByName([FromBody] string targetName)
        {
            _overViewService.LockTargetByName(targetName);

            return Ok(new { message = "target locked" });
        }
    }
}
