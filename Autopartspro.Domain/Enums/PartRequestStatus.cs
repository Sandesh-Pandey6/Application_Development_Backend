namespace Autopartspro.Domain.Enums
{
    public enum PartRequestStatus
    {
        Pending,
        Estimated,
        EscalatedToAdmin,
        Approved,
        Rejected,
        /// Admin emailed vendor to source the part.
        VendorRequested,
        /// Admin recorded the vendor purchase invoice.
        InvoiceRecorded,
    }
}