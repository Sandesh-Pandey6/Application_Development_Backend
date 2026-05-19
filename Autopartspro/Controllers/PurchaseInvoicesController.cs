using System.Security.Claims;
using Autopartspro.Application.Dtos.Admin;
using Autopartspro.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Controllers;

[ApiController]
[Route("api/purchase-invoices")]
[Authorize(Roles = "Admin,Staff")]
public class PurchaseInvoicesController : ControllerBase
{
    private readonly IPurchaseInvoiceService _purchaseInvoices;

    public PurchaseInvoicesController(IPurchaseInvoiceService purchaseInvoices)
    {
        _purchaseInvoices = purchaseInvoices;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var result = await _purchaseInvoices.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        try
        {
            var result = await _purchaseInvoices.GetByIdAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Purchase invoice not found." });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseInvoiceDto? dto)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        if (dto is null)
            return BadRequest(new { message = "Request body is required." });

        if (dto.Items is null || dto.Items.Count == 0)
            return BadRequest(new { message = "Add at least one product line." });

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m));
            return BadRequest(new { message = string.Join("; ", errors) });
        }

        try
        {
            var allowAutoCreateVendor = User.IsInRole("Admin");
            var result = await _purchaseInvoices.CreateAsync(dto, userId, allowAutoCreateVendor);
            return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateException ex)
        {
            var detail = ex.InnerException?.Message ?? ex.Message;
            return BadRequest(new { message = $"Could not save purchase to the database. {detail}" });
        }
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid id)
    {
        try
        {
            var invoice = await _purchaseInvoices.GetByIdAsync(id);
            var pdf = await _purchaseInvoices.GetPdfAsync(id);
            return File(pdf, "application/pdf", $"{invoice.InvoiceNumber}.pdf");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Purchase invoice not found." });
        }
    }
}
