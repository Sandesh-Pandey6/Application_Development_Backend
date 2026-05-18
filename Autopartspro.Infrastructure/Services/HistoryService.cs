using Autopartspro.Application.Interfaces;
using Autopartspro.Infrastructure.Data;

namespace Autopartspro.Infrastructure.Services;

public class HistoryService : IHistoryService
{
    private readonly AppDbContext _context;

    public HistoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(List<PurchaseHistoryDto> items, int total)> GetPurchaseHistoryAsync(string customerId, int pageNumber = 1, int pageSize = 10)
    {
        var items = new List<PurchaseHistoryDto>();
        return await Task.FromResult((items, 0));
    }

    public async Task<(List<ServiceHistoryDto> items, int total)> GetServiceHistoryAsync(string customerId, int pageNumber = 1, int pageSize = 10)
    {
        var items = new List<ServiceHistoryDto>();
        return await Task.FromResult((items, 0));
    }

    public async Task<(List<InvoiceHistoryDto> items, int total)> GetInvoiceHistoryAsync(string customerId, int pageNumber = 1, int pageSize = 10)
    {
        var items = new List<InvoiceHistoryDto>();
        return await Task.FromResult((items, 0));
    }
}
