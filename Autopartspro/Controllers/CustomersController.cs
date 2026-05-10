using Autopartspro.Application.DOTs.customer;
using Autopartspro.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Autopartspro.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomersController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        // ── Self Service (Customer Profile) ──

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _customerService.GetProfileAsync(userId);
            return Ok(result);
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _customerService.UpdateProfileAsync(userId, dto);
            return Ok(result);
        }

        [HttpPost("vehicles")]
        [Authorize]
        public async Task<IActionResult> AddVehicle([FromBody] CreateVehicleDto dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _customerService.AddVehicleAsync(userId, dto);
            return Ok(result);
        }

        [HttpPut("vehicles/{vehicleId}")]
        [Authorize]
        public async Task<IActionResult> UpdateVehicle(Guid vehicleId, [FromBody] UpdateVehicleDto dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _customerService.UpdateVehicleAsync(userId, vehicleId, dto);
            return Ok(result);
        }

        [HttpDelete("vehicles/{vehicleId}")]
        [Authorize]
        public async Task<IActionResult> DeleteVehicle(Guid vehicleId)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _customerService.DeleteVehicleAsync(userId, vehicleId);
            return NoContent();
        }

        // ── Staff Features ──

        [HttpGet("search")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> SearchCustomers([FromQuery] string q, [FromQuery] string by = "all")
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "Search query 'q' is required." });

            var results = await _customerService.SearchCustomersAsync(q, by);
            return Ok(results);
        }
    }
}
