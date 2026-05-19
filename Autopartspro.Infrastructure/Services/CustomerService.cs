using Autopartspro.Application.Dtos.Auth;
using Autopartspro.Application.Dtos.Customer;
using Autopartspro.Application.Interfaces;
using Autopartspro.Domain.Entities;
using Autopartspro.Domain.Enums;
using Autopartspro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Infrastructure.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly AppDbContext _context;

        public CustomerService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<UserProfileDto> GetProfileAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.Vehicles)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) throw new KeyNotFoundException("User not found.");

            return new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                City = user.City,
                Role = user.Role.ToString(),
                Status = user.Status.ToString(),
                IsEmailVerified = user.IsEmailVerified,
                MustChangePassword = user.MustChangePassword,
                ProfileImageUrl = user.ProfileImageUrl,
                Vehicles = user.Vehicles.Select(v => new VehicleDto
                {
                    Id = v.Id,
                    Make = v.Make,
                    Model = v.Model,
                    Year = v.Year,
                    FuelType = v.FuelType.ToString(),
                    NumberPlate = v.NumberPlate
                }).ToList()
            };
        }

        public async Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
        {
            var user = await _context.Users
                .Include(u => u.Vehicles)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) throw new KeyNotFoundException("User not found.");

            if (dto.FullName != null) user.FullName = dto.FullName;
            if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber;
            if (dto.City != null) user.City = dto.City;

            await _context.SaveChangesAsync();
            return await GetProfileAsync(userId);
        }

        public async Task<UserProfileDto> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
                throw new ArgumentException("New password must be at least 6 characters.");

            if (dto.NewPassword != dto.ConfirmNewPassword)
                throw new ArgumentException("New password and confirmation do not match.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) throw new KeyNotFoundException("User not found.");

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                throw new ArgumentException("Current password is incorrect.");

            if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, user.PasswordHash))
                throw new ArgumentException("Choose a password different from your current password.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.MustChangePassword = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetProfileAsync(userId);
        }

        public async Task<VehicleDto> AddVehicleAsync(Guid userId, CreateVehicleDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found.");

            var plate = dto.NumberPlate?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(plate))
                throw new ArgumentException("Number plate is required.");
            if (string.IsNullOrWhiteSpace(dto.Make) || string.IsNullOrWhiteSpace(dto.Model))
                throw new ArgumentException("Make and model are required.");

            if (await _context.Vehicles.AnyAsync(v => v.NumberPlate.ToLower() == plate.ToLower()))
                throw new ArgumentException($"Vehicle number '{plate}' is already registered.");

            if (!Enum.TryParse<FuelType>(dto.FuelType, true, out var fuelType))
            {
                fuelType = FuelType.Petrol;
            }

            var vehicle = new Vehicle
            {
                CustomerId = userId,
                Make = dto.Make.Trim(),
                Model = dto.Model.Trim(),
                Year = dto.Year,
                FuelType = fuelType,
                NumberPlate = plate
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            return new VehicleDto
            {
                Id = vehicle.Id,
                Make = vehicle.Make,
                Model = vehicle.Model,
                Year = vehicle.Year,
                FuelType = vehicle.FuelType.ToString(),
                NumberPlate = vehicle.NumberPlate
            };
        }

        public async Task<VehicleDto> UpdateVehicleAsync(Guid userId, Guid vehicleId, UpdateVehicleDto dto)
        {
            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId && v.CustomerId == userId);
            if (vehicle == null) throw new KeyNotFoundException("Vehicle not found or does not belong to user.");

            if (dto.Make != null)
            {
                if (string.IsNullOrWhiteSpace(dto.Make))
                    throw new ArgumentException("Make cannot be empty.");
                vehicle.Make = dto.Make.Trim();
            }
            if (dto.Model != null)
            {
                if (string.IsNullOrWhiteSpace(dto.Model))
                    throw new ArgumentException("Model cannot be empty.");
                vehicle.Model = dto.Model.Trim();
            }
            if (dto.Year.HasValue) vehicle.Year = dto.Year.Value;
            if (dto.FuelType != null && Enum.TryParse<FuelType>(dto.FuelType, true, out var fuelType))
            {
                vehicle.FuelType = fuelType;
            }
            if (dto.NumberPlate != null)
            {
                var plate = dto.NumberPlate.Trim();
                if (string.IsNullOrWhiteSpace(plate))
                    throw new ArgumentException("Number plate cannot be empty.");
                if (await _context.Vehicles.AnyAsync(v =>
                        v.NumberPlate.ToLower() == plate.ToLower() && v.Id != vehicleId))
                    throw new ArgumentException($"Vehicle number '{plate}' is already registered.");
                vehicle.NumberPlate = plate;
            }

            await _context.SaveChangesAsync();

            return new VehicleDto
            {
                Id = vehicle.Id,
                Make = vehicle.Make,
                Model = vehicle.Model,
                Year = vehicle.Year,
                FuelType = vehicle.FuelType.ToString(),
                NumberPlate = vehicle.NumberPlate
            };
        }

        public async Task DeleteVehicleAsync(Guid userId, Guid vehicleId)
        {
            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId && v.CustomerId == userId);
            if (vehicle == null) throw new KeyNotFoundException("Vehicle not found.");

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();
        }

        public async Task<List<CustomerSearchResultDto>> SearchCustomersAsync(string query, string searchBy)
        {
            var q = _context.Users
                .Include(u => u.Vehicles)
                .Where(u => u.Role == RoleType.Customer)
                .AsQueryable();

            switch (searchBy.ToLower())
            {
                case "vehicle":
                case "vehiclenumber":
                    q = q.Where(c => c.Vehicles.Any(v => v.NumberPlate.Contains(query)));
                    break;
                case "phone":
                    q = q.Where(c => c.PhoneNumber.Contains(query));
                    break;
                case "id":
                    if (Guid.TryParse(query, out var customerId))
                        q = q.Where(c => c.Id == customerId);
                    else
                        return new List<CustomerSearchResultDto>();
                    break;
                case "name":
                    q = q.Where(c => c.FullName.Contains(query));
                    break;
                default:
                    q = q.Where(c =>
                        c.FullName.Contains(query) ||
                        c.PhoneNumber.Contains(query) ||
                        c.Email.Contains(query) ||
                        c.Vehicles.Any(v => v.NumberPlate.Contains(query)));
                    break;
            }

            return await q.Select(c => new CustomerSearchResultDto
            {
                Id = c.Id,
                FullName = c.FullName,
                Email = c.Email,
                PhoneNumber = c.PhoneNumber,
                VehicleNumbers = c.Vehicles.Select(v => v.NumberPlate).ToList()
            }).ToListAsync();
        }
    }
}
