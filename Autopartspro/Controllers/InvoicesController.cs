using Autopartspro.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Autopartspro.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Staff")]
    public class InvoicesController : ControllerBase
    {
        private readonly ISalesInvoiceService _salesInvoiceService;

        public InvoicesController(ISalesInvoiceService salesInvoiceService)
        {
            _salesInvoiceService = salesInvoiceService;
        }

        [HttpPost("{id}/send-email")]
        public async Task<IActionResult> SendEmail(Guid id)
        {
            var result = await _salesInvoiceService.SendInvoiceEmailAsync(id);
            return Ok(new { message = "Invoice email sent successfully." });
        }
    }
}
