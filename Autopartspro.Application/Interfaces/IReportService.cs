using Autopartspro.Application.Dtos.Report;

namespace Autopartspro.Application.Interfaces
{
    public interface IReportService
    {
        Task<List<RegularCustomerReportDto>> GetRegularCustomersAsync(int minimumInvoices = 3);
        Task<List<HighSpenderReportDto>> GetHighSpendersAsync(decimal threshold = 1000);
        Task<List<PendingCreditReportDto>> GetPendingCreditsAsync();
    }
}
