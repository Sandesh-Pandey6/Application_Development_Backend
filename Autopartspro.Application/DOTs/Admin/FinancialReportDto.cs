namespace Autopartspro.Application.DOTs.admin
{
    public class FinancialReportDto
    {
        public decimal GrossRevenue { get; set; }
        public decimal RevenueChangePercent { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal ExpensesChangePercent { get; set; }
        public decimal NetMargin { get; set; }
        public decimal NetMarginChangePercent { get; set; }
        public List<RevenueOverTimeDto> SalesRevenueOverTime { get; set; } = new();
        public List<CategoryExpenseDto> PurchaseCostsByCategory { get; set; } = new();
        public List<TopSellingPartDto> TopSellingParts { get; set; } = new();
        public List<VendorSpendDto> VendorSpend { get; set; } = new();
    }

    public class RevenueOverTimeDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    public class CategoryExpenseDto
    {
        public string Category { get; set; } = string.Empty;
        public decimal TotalCost { get; set; }
    }

    public class TopSellingPartDto
    {
        public string PartName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int UnitsSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class VendorSpendDto
    {
        public string VendorName { get; set; } = string.Empty;
        public decimal TotalSpend { get; set; }
        public decimal Percentage { get; set; }
    }
}