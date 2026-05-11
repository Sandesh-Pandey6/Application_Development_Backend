namespace Autopartspro.Application.DOTs.admin
{
    public class InventoryReportDto
    {
        public int TotalPartTypes { get; set; }
        public decimal TotalStockValue { get; set; }
        public int LowStockPartsCount { get; set; }
        public List<CategoryStockDto> StockLevelsByCategory { get; set; } = new();
        public List<InventoryItemDto> FullInventoryList { get; set; } = new();
        public List<LowStockItemDto> LowStockItems { get; set; } = new();
    }

    public class CategoryStockDto
    {
        public string Category { get; set; } = string.Empty;
        public int TotalStock { get; set; }
    }

    public class InventoryItemDto
    {
        public string PartCode { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public int Stock { get; set; }
        public decimal UnitValue { get; set; }
        public decimal TotalValue { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class LowStockItemDto
    {
        public string PartName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int SuggestedReorderQty { get; set; }
        public string VendorName { get; set; } = string.Empty;
    }
}