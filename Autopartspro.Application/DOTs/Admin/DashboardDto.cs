namespace Autopartspro.Application.DOTs.admin
{
    public class DashboardDto
    {
        public decimal TotalRevenueMTD { get; set; }
        public decimal RevenueChangePercent { get; set; }
        public int TotalPartsInStock { get; set; }
        public decimal PartsChangePercent { get; set; }
        public int ActiveStaff { get; set; }
        public int StaffChangePercent { get; set; }
        public int PendingOrders { get; set; }
        public decimal PendingOrdersChangePercent { get; set; }
        public int LowStockCount { get; set; }
        public List<MonthlyRevenueDto> RevenueOverview { get; set; } = new();
        public List<RecentActivityDto> RecentActivities { get; set; } = new();
        public List<RecentPurchaseInvoiceDto> RecentPurchaseInvoices { get; set; } = new();
    }

    public class MonthlyRevenueDto
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    public class RecentActivityDto
    {
        public string StaffName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string TimeAgo { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class RecentPurchaseInvoiceDto
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}