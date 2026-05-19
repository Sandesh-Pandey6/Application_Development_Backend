using Autopartspro.Application.Dtos.Admin;

namespace Autopartspro.Application.Interfaces;

public interface IPurchaseInvoiceService
{
    Task<PurchaseInvoiceListResponseDto> GetAllAsync();
    Task<PurchaseInvoiceResponseDto> GetByIdAsync(Guid id);
    Task<PurchaseInvoiceResponseDto> CreateAsync(
        CreatePurchaseInvoiceDto dto,
        Guid recordedByUserId,
        bool allowAutoCreateVendor = false);
    Task<byte[]> GetPdfAsync(Guid id);
}
