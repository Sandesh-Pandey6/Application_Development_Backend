namespace Autopartspro.Application.DOTs.admin
{
    public class CreatePartDto
    {
        public string PartName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public Guid VendorId { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class UpdatePartDto
    {
        public string PartName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public Guid VendorId { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class PartResponseDto
    {
        public Guid Id { get; set; }
        public string PartCode { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public Guid VendorId { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsLowStock { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class PartListResponseDto
    {
        /// <summary>Sum of stock quantities across all parts.</summary>
        public int TotalParts { get; set; }
        /// <summary>Number of distinct part records (for pagination).</summary>
        public int TotalCount { get; set; }
        public int TotalCategories { get; set; }
        public int LowStockCount { get; set; }
        public int TotalVendors { get; set; }
        public List<string> Categories { get; set; } = new();
        public List<PartResponseDto> Parts { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}