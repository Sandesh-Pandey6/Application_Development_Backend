namespace Autopartspro.Application.Interfaces;

public interface IReviewService
{
    Task<int> CreateAsync(string customerId, int appointmentId, int rating, string comment);
    Task<(List<ReviewDto> items, int total)> GetAllAsync(int pageNumber = 1, int pageSize = 10);
    Task<ReviewDto?> GetByIdAsync(int id);
    Task<bool> DeleteAsync(int id);
}

public class ReviewDto
{
    public int Id { get; set; }
    public string CustomerId { get; set; } = null!;
    public int AppointmentId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
