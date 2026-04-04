using Application.Services;
using Domen.Models;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProbeScannerController : ControllerBase
    {
        private readonly ILogger<ProbeScannerController> _logger;
        private readonly ProbeScannerService _probeScannerService;

        public ProbeScannerController(
            ILogger<ProbeScannerController> logger,
            ProbeScannerService probeScannerService
        )
        {
            _logger = logger;
            _probeScannerService = probeScannerService;
        }

        [HttpGet("get-probe-scan-results")]
        public ActionResult<IEnumerable<ProbeScanItem>> GetProbeScanResults()
        {
            var overViewInfo = _probeScannerService.GetInfo();
            return Ok(overViewInfo);
        }

        [HttpPost("warp-to-anomaly")]
        public IActionResult WarpToAnomaly([FromBody] ProbeScanItem probeScanItem)
        {
            _probeScannerService.WarpToAnomaly(probeScanItem);

            return Ok(new { message = "warping to anomaly" });
        }
    }
}
