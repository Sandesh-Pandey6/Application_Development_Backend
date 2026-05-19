namespace Autopartspro.Domain.Enums
{
    public enum PartRequestStatus
    {
        Pending,
        Estimated,
        EscalatedToAdmin,
        Approved,
        Rejected,
        /// <summary>Admin emailed vendor to source the part.</summary>
        VendorRequested,
        /// <summary>Admin recorded the vendor purchase invoice.</summary>
        InvoiceRecorded,
    }
}