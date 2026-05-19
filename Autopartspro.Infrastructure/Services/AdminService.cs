using Autopartspro.Application.DOTs.admin;
using Autopartspro.Application.Interfaces;
using Autopartspro.Domain.Entities;
using Autopartspro.Domain.Enums;
using Autopartspro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Infrastructure.Services
{
    public class AdminService : IAdminService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private const int LowStockThreshold = 10;

        public AdminService(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        
        // DASHBOARD
        
        public async Task<DashboardDto> GetDashboardAsync()
        {
            var now = DateTime.UtcNow;
            var startOfMonth = UtcDate.StartOfMonth(now);
            var startOfLastMonth = startOfMonth.AddMonths(-1);

            // Revenue MTD
            var revenueMTD = await _context.SalesInvoices
                .Where(s => s.SaleDate >= startOfMonth)
                .SumAsync(s => s.TotalAmount);

            var revenueLastMonth = await _context.SalesInvoices
                .Where(s => s.SaleDate >= startOfLastMonth && s.SaleDate < startOfMonth)
                .SumAsync(s => s.TotalAmount);

            var revenueChange = revenueLastMonth > 0
                ? Math.Round((revenueMTD - revenueLastMonth) / revenueLastMonth * 100, 1)
                : 0;

            // Total Parts in Stock
            var totalParts = await _context.Parts.SumAsync(p => p.StockQuantity);

            // Active Staff
            var activeStaff = await _context.Users
                .CountAsync(u => u.Role == RoleType.Staff && u.Status == StatusType.Active);

            // Pending Orders
            var pendingOrders = await _context.PurchaseInvoices
                .CountAsync(p => p.Status == PurchaseInvoiceStatus.Pending ||
                                 p.Status == PurchaseInvoiceStatus.Processing);

            // Low Stock Count
            var lowStockCount = await _context.Parts
                .CountAsync(p => p.StockQuantity < LowStockThreshold);

            // Monthly Revenue Overview (last 7 months)
            var revenueOverview = new List<MonthlyRevenueDto>();
            for (int i = 6; i >= 0; i--)
            {
                var monthStart = UtcDate.StartOfMonth(now).AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1);
                var revenue = await _context.SalesInvoices
                    .Where(s => s.SaleDate >= monthStart && s.SaleDate < monthEnd)
                    .SumAsync(s => s.TotalAmount);

                revenueOverview.Add(new MonthlyRevenueDto
                {
                    Month = monthStart.ToString("MMM"),
                    Revenue = revenue
                });
            }

            // Recent Purchase Invoices
            var recentInvoices = await _context.PurchaseInvoices
                .Include(p => p.Vendor)
                .Include(p => p.Items)
                .OrderByDescending(p => p.PurchaseDate)
                .Take(5)
                .Select(p => new RecentPurchaseInvoiceDto
                {
                    InvoiceNumber = p.InvoiceNumber,
                    VendorName = p.Vendor.VendorName,
                    TotalItems = p.Items.Sum(i => i.Quantity),
                    TotalCost = p.TotalAmount,
                    Date = p.PurchaseDate,
                    Status = p.Status.ToString()
                })
                .ToListAsync();

            var recentSales = await _context.SalesInvoices
                .Include(s => s.Staff)
                .Include(s => s.Customer)
                .OrderByDescending(s => s.SaleDate)
                .Take(5)
                .ToListAsync();

            var recentActivities = recentSales.Select(s => new RecentActivityDto
            {
                StaffName = s.Staff?.FullName ?? "Staff",
                Action = "completed a sale for",
                Target = s.Customer?.FullName ?? "customer",
                TimeAgo = FormatTimeAgo(s.SaleDate),
                Type = "invoice"
            }).ToList();

            return new DashboardDto
            {
                TotalRevenueMTD = revenueMTD,
                RevenueChangePercent = revenueChange,
                TotalPartsInStock = totalParts,
                PartsChangePercent = 0,
                ActiveStaff = activeStaff,
                StaffChangePercent = 0,
                PendingOrders = pendingOrders,
                PendingOrdersChangePercent = 0,
                LowStockCount = lowStockCount,
                RevenueOverview = revenueOverview,
                RecentActivities = recentActivities,
                RecentPurchaseInvoices = recentInvoices
            };
        }

        
        // STAFF MANAGEMENT
        
        public async Task<StaffListResponseDto> GetAllStaffAsync(string? search)
        {
            var query = _context.Users
                .Include(u => u.StaffEmployment)
                .Where(u => u.Role == RoleType.Staff);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(u =>
                    u.FullName.Contains(search) ||
                    u.Email.Contains(search) ||
                    u.PhoneNumber.Contains(search) ||
                    (u.StaffEmployment != null &&
                     u.StaffEmployment.EmployeeId.Contains(search)));

            var staffList = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();

            var staffDtos = staffList.Select((u, index) => new StaffResponseDto
            {
                Id = u.Id,
                StaffId = u.StaffEmployment?.EmployeeId ?? $"S-{(index + 1):D3}",
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                City = u.City,
                Role = u.StaffEmployment?.AccessLevel.ToString() ?? "Staff",
                Department = u.StaffEmployment?.Department.ToString() ?? "",
                AccessLevel = u.StaffEmployment?.AccessLevel.ToString() ?? "Staff",
                BranchLocation = u.StaffEmployment?.BranchLocation ?? "",
                Status = MapStaffDisplayStatus(u),
                IsApprovedByAdmin = u.StaffEmployment?.IsApprovedByAdmin ?? false,
                CreatedAt = u.CreatedAt
            }).ToList();

            return new StaffListResponseDto
            {
                TotalStaff = staffDtos.Count,
                ActiveStaff = staffDtos.Count(s => s.Status == "Active"),
                PendingApproval = staffDtos.Count(s => s.Status == "Pending Approval"),
                Managers = staffDtos.Count(s => s.AccessLevel == "Manager"),
                InactiveStaff = staffDtos.Count(s => s.Status == "Inactive"),
                Staff = staffDtos
            };
        }

        public async Task<StaffResponseDto> GetStaffByIdAsync(Guid id)
        {
            var user = await _context.Users
                .Include(u => u.StaffEmployment)
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == RoleType.Staff)
                ?? throw new Exception("Staff member not found.");

            return new StaffResponseDto
            {
                Id = user.Id,
                StaffId = user.StaffEmployment?.EmployeeId ?? "",
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                City = user.City,
                Role = user.StaffEmployment?.AccessLevel.ToString() ?? "Staff",
                Department = user.StaffEmployment?.Department.ToString() ?? "",
                AccessLevel = user.StaffEmployment?.AccessLevel.ToString() ?? "Staff",
                BranchLocation = user.StaffEmployment?.BranchLocation ?? "",
                Status = MapStaffDisplayStatus(user),
                IsApprovedByAdmin = user.StaffEmployment?.IsApprovedByAdmin ?? false,
                CreatedAt = user.CreatedAt
            };
        }

        private static string MapStaffDisplayStatus(User u)
        {
            if (u.Status == StatusType.Inactive)
                return "Inactive";
            if (u.StaffEmployment != null && !u.StaffEmployment.IsApprovedByAdmin)
                return "Pending Approval";
            return "Active";
        }

        public async Task<StaffResponseDto> CreateStaffAsync(CreateStaffDto dto)
        {
            var exists = await _context.Users
                .AnyAsync(u => u.Email == dto.Email);
            if (exists) throw new Exception("Email already exists.");

            var empExists = await _context.StaffEmployments
                .AnyAsync(e => e.EmployeeId == dto.EmployeeId);
            if (empExists) throw new Exception("Employee ID already exists.");

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                PhoneNumber = dto.PhoneNumber,
                City = dto.City,
                Role = RoleType.Staff,
                Status = StatusType.Active,
                IsEmailVerified = true
            };

            _context.Users.Add(user);

            var department = Enum.TryParse<Department>(dto.Department, out var dep)
                ? dep : Department.Sales;
            var accessLevel = Enum.TryParse<AccessLevel>(dto.AccessLevel, out var al)
                ? al : AccessLevel.Staff;

            var employment = new StaffEmployment
            {
                UserId = user.Id,
                EmployeeId = dto.EmployeeId,
                Department = department,
                AccessLevel = accessLevel,
                BranchLocation = dto.BranchLocation,
                IsApprovedByAdmin = true
            };

            _context.StaffEmployments.Add(employment);

            // Notify admin
            _context.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Message = $"Staff account for {user.FullName} ({dto.Department} Dept.) was created by Admin.",
                Type = NotificationType.General,
                IsRead = false
            });

            await _context.SaveChangesAsync();

            await _emailService.SendStaffApprovalEmailAsync(user.Email, user.FullName);

            return await GetStaffByIdAsync(user.Id);
        }

        public async Task<StaffResponseDto> UpdateStaffAsync(Guid id, UpdateStaffDto dto)
        {
            var user = await _context.Users
                .Include(u => u.StaffEmployment)
                .FirstOrDefaultAsync(u => u.Id == id)
                ?? throw new Exception("Staff not found.");

            user.FullName = dto.FullName;
            user.PhoneNumber = dto.PhoneNumber;
            user.City = dto.City;
            user.UpdatedAt = DateTime.UtcNow;

            if (user.StaffEmployment != null)
            {
                user.StaffEmployment.Department = Enum.TryParse<Department>(
                    dto.Department, out var dep) ? dep : user.StaffEmployment.Department;
                user.StaffEmployment.AccessLevel = Enum.TryParse<AccessLevel>(
                    dto.AccessLevel, out var al) ? al : user.StaffEmployment.AccessLevel;
                user.StaffEmployment.BranchLocation = dto.BranchLocation;

                if (dto.IsApprovedByAdmin.HasValue)
                {
                    var wasApproved = user.StaffEmployment.IsApprovedByAdmin;
                    user.StaffEmployment.IsApprovedByAdmin = dto.IsApprovedByAdmin.Value;
                    if (dto.IsApprovedByAdmin.Value && !wasApproved)
                    {
                        await _emailService.SendStaffApprovalEmailAsync(user.Email, user.FullName);
                    }
                }

                user.StaffEmployment.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return await GetStaffByIdAsync(id);
        }

        public async Task<string> ToggleStaffStatusAsync(Guid id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id)
                ?? throw new Exception("Staff not found.");

            user.Status = user.Status == StatusType.Active
                ? StatusType.Inactive
                : StatusType.Active;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return $"Staff {user.Status.ToString().ToLower()} successfully.";
        }

        public async Task<string> ApproveStaffAsync(Guid id)
        {
            var employment = await _context.StaffEmployments
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.UserId == id)
                ?? throw new Exception("Staff employment not found.");

            employment.IsApprovedByAdmin = true;
            employment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _emailService.SendStaffApprovalEmailAsync(
                employment.User.Email, employment.User.FullName);

            return "Staff account approved successfully.";
        }

        public async Task<string> RejectStaffAsync(Guid id)
        {
            var user = await _context.Users
                .Include(u => u.StaffEmployment)
                .FirstOrDefaultAsync(u => u.Id == id)
                ?? throw new Exception("Staff not found.");

            user.Status = StatusType.Inactive;
            await _context.SaveChangesAsync();

            await _emailService.SendStaffRejectionEmailAsync(user.Email, user.FullName);
            return "Staff account rejected.";
        }

        
        // PARTS & INVENTORY
       
        public async Task<PartListResponseDto> GetAllPartsAsync(string? search,
            string? category, string? stockLevel, int page, int pageSize)
        {
            var query = _context.Parts.Include(p => p.Vendor).AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p =>
                    p.PartName.Contains(search) ||
                    p.Category.Contains(search));

            if (!string.IsNullOrEmpty(category) && category != "All Categories")
                query = query.Where(p => p.Category == category);

            if (stockLevel == "Low Stock")
                query = query.Where(p => p.StockQuantity < LowStockThreshold);
            else if (stockLevel == "In Stock")
                query = query.Where(p => p.StockQuantity >= LowStockThreshold);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var parts = await query
                .OrderByDescending(p => p.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var partDtos = parts.Select((p, index) => new PartResponseDto
            {
                Id = p.Id,
                PartCode = $"P-{(index + 1 + (page - 1) * pageSize):D3}",
                PartName = p.PartName,
                Description = p.Description,
                Category = p.Category,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                VendorName = p.Vendor?.VendorName ?? "",
                VendorId = p.VendorId,
                ImageUrl = p.ImageUrl,
                IsLowStock = p.StockQuantity < LowStockThreshold,
                UpdatedAt = p.UpdatedAt
            }).ToList();

            var categories = await _context.Parts
                .Select(p => p.Category)
                .Where(c => c != null && c != "")
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return new PartListResponseDto
            {
                TotalParts = await _context.Parts.SumAsync(p => p.StockQuantity),
                TotalCount = totalCount,
                TotalCategories = categories.Count,
                LowStockCount = await _context.Parts
                    .CountAsync(p => p.StockQuantity < LowStockThreshold),
                TotalVendors = await _context.Vendors.CountAsync(),
                Categories = categories,
                Parts = partDtos,
                CurrentPage = page,
                TotalPages = totalPages
            };
        }

        public async Task<PartResponseDto> GetPartByIdAsync(Guid id)
        {
            var part = await _context.Parts
                .Include(p => p.Vendor)
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new Exception("Part not found.");

            return new PartResponseDto
            {
                Id = part.Id,
                PartName = part.PartName,
                Description = part.Description,
                Category = part.Category,
                Price = part.Price,
                StockQuantity = part.StockQuantity,
                VendorName = part.Vendor?.VendorName ?? "",
                VendorId = part.VendorId,
                ImageUrl = part.ImageUrl,
                IsLowStock = part.StockQuantity < LowStockThreshold,
                UpdatedAt = part.UpdatedAt
            };
        }

        public async Task<PartResponseDto> CreatePartAsync(CreatePartDto dto)
        {
            var part = new Part
            {
                PartName = dto.PartName,
                Description = dto.Description,
                Category = dto.Category,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                VendorId = dto.VendorId,
                ImageUrl = dto.ImageUrl
            };

            _context.Parts.Add(part);

            // Auto low stock notification
            if (dto.StockQuantity < LowStockThreshold)
                await CreateLowStockNotificationAsync(dto.PartName, dto.StockQuantity);

            await _context.SaveChangesAsync();
            return await GetPartByIdAsync(part.Id);
        }

        public async Task<PartResponseDto> UpdatePartAsync(Guid id, UpdatePartDto dto)
        {
            var part = await _context.Parts.FindAsync(id)
                ?? throw new Exception("Part not found.");

            part.PartName = dto.PartName;
            part.Description = dto.Description;
            part.Category = dto.Category;
            part.Price = dto.Price;
            part.StockQuantity = dto.StockQuantity;
            part.VendorId = dto.VendorId;
            part.ImageUrl = dto.ImageUrl;
            part.UpdatedAt = DateTime.UtcNow;

            // Auto low stock notification
            if (dto.StockQuantity < LowStockThreshold)
                await CreateLowStockNotificationAsync(dto.PartName, dto.StockQuantity);

            await _context.SaveChangesAsync();
            return await GetPartByIdAsync(id);
        }

        public async Task<string> DeletePartAsync(Guid id)
        {
            var part = await _context.Parts.FindAsync(id)
                ?? throw new Exception("Part not found.");

            _context.Parts.Remove(part);
            await _context.SaveChangesAsync();
            return "Part deleted successfully.";
        }

        
        // PURCHASE INVOICES
        
        public async Task<PurchaseInvoiceListResponseDto> GetAllPurchaseInvoicesAsync()
        {
            var invoices = await _context.PurchaseInvoices
                .Include(p => p.Vendor)
                .Include(p => p.Items)
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();

            var invoiceDtos = invoices.Select(p => new PurchaseInvoiceResponseDto
            {
                Id = p.Id,
                InvoiceNumber = p.InvoiceNumber,
                VendorName = p.Vendor.VendorName,
                VendorId = p.VendorId,
                TotalItems = p.Items.Sum(i => i.Quantity),
                TotalAmount = p.TotalAmount,
                PurchaseDate = p.PurchaseDate,
                Status = p.Status.ToString()
            }).ToList();

            return new PurchaseInvoiceListResponseDto
            {
                TotalInvoices = invoiceDtos.Count,
                TotalValue = invoiceDtos.Sum(i => i.TotalAmount),
                Completed = invoiceDtos.Count(i => i.Status == "Completed"),
                PendingOrProcessing = invoiceDtos.Count(i =>
                    i.Status == "Pending" || i.Status == "Processing"),
                Invoices = invoiceDtos
            };
        }

        public async Task<PurchaseInvoiceResponseDto> GetPurchaseInvoiceByIdAsync(Guid id)
        {
            var invoice = await _context.PurchaseInvoices
                .Include(p => p.Vendor)
                .Include(p => p.Items).ThenInclude(i => i.Part)
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new Exception("Invoice not found.");

            return new PurchaseInvoiceResponseDto
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                VendorName = invoice.Vendor.VendorName,
                VendorId = invoice.VendorId,
                TotalItems = invoice.Items.Sum(i => i.Quantity),
                TotalAmount = invoice.TotalAmount,
                PurchaseDate = invoice.PurchaseDate,
                Status = invoice.Status.ToString(),
                Items = invoice.Items.Select(i => new PurchaseInvoiceItemResponseDto
                {
                    PartName = i.Part.PartName,
                    Category = i.Part.Category,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    SubTotal = i.SubTotal
                }).ToList()
            };
        }

        public async Task<PurchaseInvoiceResponseDto> CreatePurchaseInvoiceAsync(
            CreatePurchaseInvoiceDto dto, Guid adminId)
        {
            var vendor = await _context.Vendors.FindAsync(dto.VendorId)
                ?? throw new Exception("Vendor not found.");

            // Generate invoice number PI-YYYY-XXX
            var count = await _context.PurchaseInvoices.CountAsync();
            var invoiceNumber = $"PI-{DateTime.UtcNow.Year}-{(count + 1):D3}";

            decimal totalAmount = 0;
            var invoiceItems = new List<PurchaseInvoiceItem>();

            foreach (var item in dto.Items)
            {
                var part = await _context.Parts.FindAsync(item.PartId)
                    ?? throw new Exception($"Part not found: {item.PartId}");

                var subTotal = item.Quantity * item.UnitPrice;
                totalAmount += subTotal;

                invoiceItems.Add(new PurchaseInvoiceItem
                {
                    PartId = item.PartId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    SubTotal = subTotal
                });

                // Update stock
                part.StockQuantity += item.Quantity;
                part.UpdatedAt = DateTime.UtcNow;
            }

            var invoice = new PurchaseInvoice
            {
                InvoiceNumber = invoiceNumber,
                VendorId = dto.VendorId,
                AdminId = adminId,
                TotalAmount = totalAmount,
                PurchaseDate = dto.PurchaseDate,
                Status = PurchaseInvoiceStatus.Pending,
                Items = invoiceItems
            };

            _context.PurchaseInvoices.Add(invoice);
            await _context.SaveChangesAsync();

            return await GetPurchaseInvoiceByIdAsync(invoice.Id);
        }

        public async Task<string> UpdatePurchaseInvoiceStatusAsync(Guid id, string status)
        {
            var invoice = await _context.PurchaseInvoices.FindAsync(id)
                ?? throw new Exception("Invoice not found.");

            invoice.Status = Enum.TryParse<PurchaseInvoiceStatus>(status, out var s)
                ? s : invoice.Status;

            await _context.SaveChangesAsync();
            return $"Invoice status updated to {status}.";
        }

        
        // FINANCIAL REPORTS
        
        public async Task<FinancialReportDto> GetFinancialReportAsync(
            string period, DateTime? date)
        {
            var now = UtcDate.EnsureUtc(date ?? DateTime.UtcNow);
            DateTime startDate, endDate, prevStart, prevEnd;

            switch (period.ToLower())
            {
                case "daily":
                    startDate = UtcDate.StartOfDay(now);
                    endDate = startDate.AddDays(1);
                    prevStart = startDate.AddDays(-1);
                    prevEnd = startDate;
                    break;
                case "yearly":
                    startDate = UtcDate.StartOfYear(now);
                    endDate = startDate.AddYears(1);
                    prevStart = startDate.AddYears(-1);
                    prevEnd = startDate;
                    break;
                default: // monthly
                    startDate = UtcDate.StartOfMonth(now);
                    endDate = startDate.AddMonths(1);
                    prevStart = startDate.AddMonths(-1);
                    prevEnd = startDate;
                    break;
            }

            // Revenue
            var grossRevenue = await _context.SalesInvoices
                .Where(s => s.SaleDate >= startDate && s.SaleDate < endDate)
                .SumAsync(s => s.TotalAmount);

            var prevRevenue = await _context.SalesInvoices
                .Where(s => s.SaleDate >= prevStart && s.SaleDate < prevEnd)
                .SumAsync(s => s.TotalAmount);

            var revenueChange = prevRevenue > 0
                ? Math.Round((grossRevenue - prevRevenue) / prevRevenue * 100, 1) : 0;

            // Expenses
            var totalExpenses = await _context.PurchaseInvoices
                .Where(p => p.PurchaseDate >= startDate && p.PurchaseDate < endDate)
                .SumAsync(p => p.TotalAmount);

            var prevExpenses = await _context.PurchaseInvoices
                .Where(p => p.PurchaseDate >= prevStart && p.PurchaseDate < prevEnd)
                .SumAsync(p => p.TotalAmount);

            var expensesChange = prevExpenses > 0
                ? Math.Round((totalExpenses - prevExpenses) / prevExpenses * 100, 1) : 0;

            // Net Margin
            var netMargin = grossRevenue > 0
                ? Math.Round((grossRevenue - totalExpenses) / grossRevenue * 100, 1) : 0;

            var prevNetMargin = prevRevenue > 0
                ? Math.Round((prevRevenue - prevExpenses) / prevRevenue * 100, 1) : 0;

            var netMarginChange = prevNetMargin > 0
                ? Math.Round(netMargin - prevNetMargin, 1) : 0;

            // Revenue over time (format dates in memory — EF cannot translate ToString("dd MMM"))
            var salesInPeriod = await _context.SalesInvoices
                .Where(s => s.SaleDate >= startDate && s.SaleDate < endDate)
                .Select(s => new { s.SaleDate, s.TotalAmount })
                .ToListAsync();

            var salesOverTime = salesInPeriod
                .GroupBy(s => DateOnly.FromDateTime(s.SaleDate))
                .OrderBy(g => g.Key)
                .Select(g => new RevenueOverTimeDto
                {
                    Label = g.Key.ToString("dd MMM"),
                    Revenue = g.Sum(x => x.TotalAmount)
                })
                .ToList();

            // Purchase costs by category
            var costsByCategory = await _context.PurchaseInvoiceItems
                .Include(i => i.Part)
                .Include(i => i.PurchaseInvoice)
                .Where(i => i.PurchaseInvoice.PurchaseDate >= startDate &&
                            i.PurchaseInvoice.PurchaseDate < endDate)
                .GroupBy(i => i.Part.Category)
                .Select(g => new CategoryExpenseDto
                {
                    Category = g.Key,
                    TotalCost = g.Sum(i => i.SubTotal)
                })
                .OrderByDescending(c => c.TotalCost)
                .ToListAsync();

            // Top selling parts
            var topParts = await _context.SalesInvoiceItems
                .Include(i => i.Part)
                .Include(i => i.SalesInvoice)
                .Where(i => i.SalesInvoice.SaleDate >= startDate &&
                            i.SalesInvoice.SaleDate < endDate)
                .GroupBy(i => new { i.PartId, i.Part.PartName, i.Part.Category })
                .Select(g => new TopSellingPartDto
                {
                    PartName = g.Key.PartName,
                    Category = g.Key.Category,
                    UnitsSold = g.Sum(i => i.Quantity),
                    TotalRevenue = g.Sum(i => i.SubTotal)
                })
                .OrderByDescending(p => p.TotalRevenue)
                .Take(5)
                .ToListAsync();

            // Vendor spend
            var totalVendorSpend = await _context.PurchaseInvoices
                .Where(p => p.PurchaseDate >= startDate && p.PurchaseDate < endDate)
                .SumAsync(p => p.TotalAmount);

            var vendorSpend = await _context.PurchaseInvoices
                .Include(p => p.Vendor)
                .Where(p => p.PurchaseDate >= startDate && p.PurchaseDate < endDate)
                .GroupBy(p => new { p.VendorId, p.Vendor.VendorName })
                .Select(g => new VendorSpendDto
                {
                    VendorName = g.Key.VendorName,
                    TotalSpend = g.Sum(p => p.TotalAmount),
                    Percentage = totalVendorSpend > 0
                        ? Math.Round(g.Sum(p => p.TotalAmount) / totalVendorSpend * 100, 1)
                        : 0
                })
                .OrderByDescending(v => v.TotalSpend)
                .ToListAsync();

            return new FinancialReportDto
            {
                GrossRevenue = grossRevenue,
                RevenueChangePercent = revenueChange,
                TotalExpenses = totalExpenses,
                ExpensesChangePercent = expensesChange,
                NetMargin = netMargin,
                NetMarginChangePercent = netMarginChange,
                SalesRevenueOverTime = salesOverTime,
                PurchaseCostsByCategory = costsByCategory,
                TopSellingParts = topParts,
                VendorSpend = vendorSpend
            };
        }

        
        // INVENTORY REPORTS
        
        public async Task<InventoryReportDto> GetInventoryReportAsync()
        {
            var parts = await _context.Parts
                .Include(p => p.Vendor)
                .OrderBy(p => p.Category)
                .ToListAsync();

            var stockByCategory = parts
                .GroupBy(p => p.Category)
                .Select(g => new CategoryStockDto
                {
                    Category = g.Key,
                    TotalStock = g.Sum(p => p.StockQuantity)
                })
                .OrderByDescending(c => c.TotalStock)
                .ToList();

            var inventoryList = parts.Select((p, index) => new InventoryItemDto
            {
                PartCode = $"P-{(index + 1):D3}",
                PartName = p.PartName,
                Category = p.Category,
                VendorName = p.Vendor?.VendorName ?? "",
                Stock = p.StockQuantity,
                UnitValue = p.Price,
                TotalValue = p.Price * p.StockQuantity,
                Status = p.StockQuantity < LowStockThreshold ? "Low Stock" : "In Stock"
            }).ToList();

            var lowStockItems = parts
                .Where(p => p.StockQuantity < LowStockThreshold)
                .Select(p => new LowStockItemDto
                {
                    PartName = p.PartName,
                    Category = p.Category,
                    CurrentStock = p.StockQuantity,
                    SuggestedReorderQty = (LowStockThreshold * 5) + (10 - p.StockQuantity),
                    VendorName = p.Vendor?.VendorName ?? ""
                }).ToList();

            return new InventoryReportDto
            {
                TotalPartTypes = parts.Count,
                TotalStockValue = parts.Sum(p => p.Price * p.StockQuantity),
                LowStockPartsCount = lowStockItems.Count,
                StockLevelsByCategory = stockByCategory,
                FullInventoryList = inventoryList,
                LowStockItems = lowStockItems
            };
        }

        
        // NOTIFICATIONS
        
        public async Task<NotificationListDto> GetAllNotificationsAsync(
            Guid adminId, string? type)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == adminId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(type) && type != "All")
            {
                var notifType = MapNotificationFilterType(type);
                if (notifType.HasValue)
                    query = query.Where(n => n.Type == notifType.Value);
            }

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var all = await _context.Notifications
                .Where(n => n.UserId == adminId)
                .ToListAsync();

            return new NotificationListDto
            {
                TotalUnread = all.Count(n => !n.IsRead),
                LowStockCount = all.Count(n => n.Type == NotificationType.LowStock && !n.IsRead),
                CreditReminderCount = all.Count(n => n.Type == NotificationType.CreditReminder && !n.IsRead),
                InfoCount = all.Count(n => n.Type == NotificationType.General && !n.IsRead),
                Notifications = notifications.Select(n => new NotificationResponseDto
                {
                    Id = n.Id,
                    Title = GetNotificationTitle(n),
                    Message = n.Message,
                    Type = MapNotificationDisplayType(n.Type),
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                }).ToList()
            };
        }

        public async Task<string> MarkNotificationAsReadAsync(Guid notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId)
                ?? throw new Exception("Notification not found.");

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return "Notification marked as read.";
        }

        public async Task<string> MarkAllNotificationsAsReadAsync(Guid adminId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == adminId && !n.IsRead)
                .ToListAsync();

            foreach (var n in notifications)
                n.IsRead = true;

            await _context.SaveChangesAsync();
            return "All notifications marked as read.";
        }

        
        // HELPERS
        
        private async Task CreateLowStockNotificationAsync(string partName, int stock)
        {
            var admin = await _context.Users
                .FirstOrDefaultAsync(u => u.Role == RoleType.Admin);

            if (admin == null) return;

            _context.Notifications.Add(new Notification
            {
                UserId = admin.Id,
                Message = $"{partName} stock has dropped to {stock} units — below the minimum threshold of 10. Immediate reorder recommended.",
                Type = NotificationType.LowStock,
                IsRead = false
            });
        }

        private static string GetNotificationTitle(Notification n) => n.Type switch
        {
            NotificationType.LowStock => $"Low Stock Alert",
            NotificationType.CreditReminder => "Credit Reminder",
            NotificationType.General => "System Info",
            _ => "Notification"
        };

        private static string MapNotificationDisplayType(NotificationType type) => type switch
        {
            NotificationType.LowStock => "Low Stock",
            NotificationType.CreditReminder => "Credit Reminder",
            NotificationType.General => "Info",
            _ => "Info"
        };

        private static NotificationType? MapNotificationFilterType(string type) => type switch
        {
            "Low Stock" => NotificationType.LowStock,
            "Credit Reminder" => NotificationType.CreditReminder,
            "Info" => NotificationType.General,
            _ when Enum.TryParse<NotificationType>(type, true, out var parsed) => parsed,
            _ => null
        };

        private static string FormatTimeAgo(DateTime utc)
        {
            var span = DateTime.UtcNow - utc;
            if (span.TotalMinutes < 1) return "just now";
            if (span.TotalHours < 1) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalDays < 1) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 30) return $"{(int)span.TotalDays}d ago";
            return utc.ToString("dd MMM yyyy");
        }
    }
}