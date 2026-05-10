using Autopartspro.Domain.Enums;

namespace Autopartspro.Domain.Entities
{
    public class OtpVerification
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
        public OtpPurpose Purpose { get; set; }
        public bool IsUsed { get; set; } = false;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}