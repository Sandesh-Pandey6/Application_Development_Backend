using Autopartspro.Application.Dtos.Admin;
using Autopartspro.Application.Interfaces;
using Autopartspro.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Autopartspro.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly IAdminProfileService _adminProfile;
        private readonly IUserProfileImageService _profileImages;

        public AdminController(
            IAdminService adminService,
            IAdminProfileService adminProfile,
            IUserProfileImageService profileImages)
        {
            _adminService = adminService;
            _adminProfile = adminProfile;
            _profileImages = profileImages;
        }

        private Guid GetAdminId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Dashboard 
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var result = await _adminService.GetDashboardAsync();
            return Ok(result);
        }

        // asatai Staff Management 
        [HttpGet("staff")]
        public async Task<IActionResult> GetAllStaff([FromQuery] string? search)
        {
            var result = await _adminService.GetAllStaffAsync(search);
            return Ok(result);
        }

        [HttpGet("staff/{id}")]
        public async Task<IActionResult> GetStaffById(Guid id)
        {
            var result = await _adminService.GetStaffByIdAsync(id);
            return Ok(result);
        }

        [HttpPost("staff")]
        public async Task<IActionResult> CreateStaff([FromBody] CreateStaffDto dto)
        {
            var result = await _adminService.CreateStaffAsync(dto);
            return Ok(result);
        }

        [HttpPut("staff/{id}")]
        public async Task<IActionResult> UpdateStaff(Guid id, [FromBody] UpdateStaffDto dto)
        {
            var result = await _adminService.UpdateStaffAsync(id, dto);
            return Ok(result);
        }

        [HttpPatch("staff/{id}/toggle-status")]
        public async Task<IActionResult> ToggleStaffStatus(Guid id)
        {
            var result = await _adminService.ToggleStaffStatusAsync(id);
            return Ok(new { message = result });
        }

        [HttpPatch("staff/{id}/approve")]
        public async Task<IActionResult> ApproveStaff(Guid id)
        {
            var result = await _adminService.ApproveStaffAsync(id);
            return Ok(new { message = result });
        }

        [HttpPatch("staff/{id}/reject")]
        public async Task<IActionResult> RejectStaff(Guid id)
        {
            var result = await _adminService.RejectStaffAsync(id);
            return Ok(new { message = result });
        }

        //  Parts & Inventory 
        [HttpGet("parts")]
        public async Task<IActionResult> GetAllParts(
            [FromQuery] string? search,
            [FromQuery] string? category,
            [FromQuery] string? stockLevel,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 8)
        {
            var result = await _adminService.GetAllPartsAsync(
                search, category, stockLevel, page, pageSize);
            return Ok(result);
        }

        [HttpGet("parts/{id}")]
        public async Task<IActionResult> GetPartById(Guid id)
        {
            var result = await _adminService.GetPartByIdAsync(id);
            return Ok(result);
        }

        [HttpPost("parts")]
        public async Task<IActionResult> CreatePart([FromBody] CreatePartDto dto)
        {
            var result = await _adminService.CreatePartAsync(dto);
            return Ok(result);
        }

        [HttpPut("parts/{id}")]
        public async Task<IActionResult> UpdatePart(Guid id, [FromBody] UpdatePartDto dto)
        {
            var result = await _adminService.UpdatePartAsync(id, dto);
            return Ok(result);
        }

        [HttpDelete("parts/{id}")]
        public async Task<IActionResult> DeletePart(Guid id)
        {
            var result = await _adminService.DeletePartAsync(id);
            return Ok(new { message = result });
        }

        //  Purchase Invoices 
        [HttpGet("purchase-invoices")]
        public async Task<IActionResult> GetAllPurchaseInvoices()
        {
            var result = await _adminService.GetAllPurchaseInvoicesAsync();
            return Ok(result);
        }

        [HttpGet("purchase-invoices/{id}")]
        public async Task<IActionResult> GetPurchaseInvoiceById(Guid id)
        {
            var result = await _adminService.GetPurchaseInvoiceByIdAsync(id);
            return Ok(result);
        }

        [HttpPost("purchase-invoices")]
        public async Task<IActionResult> CreatePurchaseInvoice(
            [FromBody] CreatePurchaseInvoiceDto dto)
        {
            var result = await _adminService.CreatePurchaseInvoiceAsync(dto, GetAdminId());
            return Ok(result);
        }

        [HttpPatch("purchase-invoices/{id}/status")]
        public async Task<IActionResult> UpdateInvoiceStatus(
            Guid id, [FromQuery] string status)
        {
            var result = await _adminService.UpdatePurchaseInvoiceStatusAsync(id, status);
            return Ok(new { message = result });
        }

        //  Financial Reports
        [HttpGet("financial-reports")]
        public async Task<IActionResult> GetFinancialReport(
            [FromQuery] string period = "monthly",
            [FromQuery] DateTime? date = null)
        {
            var result = await _adminService.GetFinancialReportAsync(period, date);
            return Ok(result);
        }

        [HttpGet("financial-reports/pdf")]
        public async Task<IActionResult> DownloadFinancialReportPdf(
            [FromQuery] string period = "monthly",
            [FromQuery] DateTime? date = null)
        {
            var pdf = await _adminService.GetFinancialReportPdfAsync(period, date);
            var fileName = $"financial-report-{period.ToLower()}-{DateTime.UtcNow:yyyyMMdd}.pdf";
            return File(pdf, "application/pdf", fileName);
        }

        [HttpGet("unpaid-sales-invoices")]
        public async Task<IActionResult> GetUnpaidSalesInvoices([FromQuery] string? filter = "all")
        {
            var result = await _adminService.GetUnpaidSalesInvoicesAsync(filter);
            return Ok(result);
        }

        [HttpPatch("unpaid-sales-invoices/{id}/mark-paid")]
        public async Task<IActionResult> MarkSalesInvoicePaid(Guid id)
        {
            var message = await _adminService.MarkSalesInvoicePaidAsync(id);
            return Ok(new { message });
        }

        [HttpGet("sales-invoices/{id}/pdf")]
        public async Task<IActionResult> DownloadSalesInvoicePdf(Guid id)
        {
            try
            {
                var pdf = await _adminService.GetSalesInvoicePdfAsync(id);
                return File(pdf, "application/pdf", $"sales-invoice-{id}.pdf");
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Sales invoice not found." });
            }
        }

        //  Inventory Reports 
        [HttpGet("inventory-reports")]
        public async Task<IActionResult> GetInventoryReport()
        {
            var result = await _adminService.GetInventoryReportAsync();
            return Ok(result);
        }

        //  Notifications 
        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications([FromQuery] string? type)
        {
            var result = await _adminService.GetAllNotificationsAsync(GetAdminId(), type);
            return Ok(result);
        }

        [HttpPatch("notifications/{id}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var result = await _adminService.MarkNotificationAsReadAsync(id, GetAdminId());
            return Ok(new { message = result });
        }

        [HttpPatch("notifications/read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var result = await _adminService.MarkAllNotificationsAsReadAsync(GetAdminId());
            return Ok(new { message = result });
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var profile = await _adminProfile.GetProfileAsync(GetAdminId());
                return Ok(profile);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateAdminProfileDto dto)
        {
            try
            {
                var profile = await _adminProfile.UpdateProfileAsync(GetAdminId(), dto);
                return Ok(profile);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("profile/photo")]
        [RequestSizeLimit(ImageUploadRules.MaxBytes)]
        public async Task<IActionResult> UploadProfilePhoto(IFormFile? file)
        {
            try
            {
                ImageUploadRules.Validate(file);
                await using var stream = file!.OpenReadStream();
                await _profileImages.UploadAsync(
                    GetAdminId(),
                    stream,
                    file.FileName,
                    file.ContentType);
                return Ok(await _adminProfile.GetProfileAsync(GetAdminId()));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("profile/photo")]
        public async Task<IActionResult> DeleteProfilePhoto()
        {
            try
            {
                await _profileImages.RemoveAsync(GetAdminId());
                return Ok(await _adminProfile.GetProfileAsync(GetAdminId()));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
