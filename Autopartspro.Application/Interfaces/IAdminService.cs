using Autopartspro.Application.DOTs.admin;

namespace Autopartspro.Application.Interfaces
{
    public interface IAdminService
    {
        // Dashboard
        Task<DashboardDto> GetDashboardAsync();

        // Staff Management
        Task<StaffListResponseDto> GetAllStaffAsync(string? search);
        Task<StaffResponseDto> GetStaffByIdAsync(Guid id);
        Task<StaffResponseDto> CreateStaffAsync(CreateStaffDto dto);
        Task<StaffResponseDto> UpdateStaffAsync(Guid id, UpdateStaffDto dto);
        Task<string> ToggleStaffStatusAsync(Guid id);
        Task<string> ApproveStaffAsync(Guid id);
        Task<string> RejectStaffAsync(Guid id);

        // Parts & Inventory
        Task<PartListResponseDto> GetAllPartsAsync(string? search, string? category,
            string? stockLevel, int page, int pageSize);
        Task<PartResponseDto> GetPartByIdAsync(Guid id);
        Task<PartResponseDto> CreatePartAsync(CreatePartDto dto);
        Task<PartResponseDto> UpdatePartAsync(Guid id, UpdatePartDto dto);
        Task<string> DeletePartAsync(Guid id);

        // Purchase Invoices
        Task<PurchaseInvoiceListResponseDto> GetAllPurchaseInvoicesAsync();
        Task<PurchaseInvoiceResponseDto> GetPurchaseInvoiceByIdAsync(Guid id);
        Task<PurchaseInvoiceResponseDto> CreatePurchaseInvoiceAsync(
            CreatePurchaseInvoiceDto dto, Guid adminId);
        Task<string> UpdatePurchaseInvoiceStatusAsync(Guid id, string status);

        // Financial Reports
        Task<FinancialReportDto> GetFinancialReportAsync(string period, DateTime? date);

        // Inventory Reports
        Task<InventoryReportDto> GetInventoryReportAsync();

        // Notifications
        Task<NotificationListDto> GetAllNotificationsAsync(Guid adminId, string? type);
        Task<string> MarkNotificationAsReadAsync(Guid notificationId);
        Task<string> MarkAllNotificationsAsReadAsync(Guid adminId);
    }
}