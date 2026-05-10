using Autopartspro.Application.Interfaces;
using Autopartspro.Infrastructure.Data;

namespace Autopartspro.Infrastructure.Services;

public class ReviewService : IReviewService
{
    private readonly AppDbContext _context;

    public ReviewService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateAsync(string customerId, int appointmentId, int rating, string comment)
    {
        return await Task.FromResult(1);
    }

    public async Task<(List<ReviewDto> items, int total)> GetAllAsync(int pageNumber = 1, int pageSize = 10)
    {
        var items = new List<ReviewDto>();
        return await Task.FromResult((items, 0));
    }

    public async Task<ReviewDto?> GetByIdAsync(int id)
    {
        return await Task.FromResult<ReviewDto?>(null);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await Task.FromResult(true);
    }
}
