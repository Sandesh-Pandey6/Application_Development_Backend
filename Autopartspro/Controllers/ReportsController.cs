using Autopartspro.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Autopartspro.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Staff")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("regulars")]
        public async Task<IActionResult> GetRegulars([FromQuery] int minimumInvoices = 3)
        {
            var result = await _reportService.GetRegularCustomersAsync(minimumInvoices);
            return Ok(result);
        }

        [HttpGet("high-spenders")]
        public async Task<IActionResult> GetHighSpenders([FromQuery] decimal threshold = 1000)
        {
            var result = await _reportService.GetHighSpendersAsync(threshold);
            return Ok(result);
        }

        [HttpGet("pending-credits")]
        public async Task<IActionResult> GetPendingCredits()
        {
            var result = await _reportService.GetPendingCreditsAsync();
            return Ok(result);
        }
    }
}
