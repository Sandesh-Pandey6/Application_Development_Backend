using Autopartspro.Application.Dtos.PartRequests;
using Autopartspro.Domain.Entities;

namespace Autopartspro.Application.Interfaces;

public interface IPartRequestAdminService
{
    Task<PartRequest> RequestVendorAsync(Guid partRequestId, Guid adminUserId, RequestVendorForPartDto dto);
    Task<PartRequest> RecordVendorInvoiceAsync(Guid partRequestId, Guid adminUserId, RecordPartRequestVendorInvoiceDto dto);
}
