namespace Autopartspro.Application.DOTs.auth
{
    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        /// <summary>Expected portal role: Customer, Staff, or Admin (from login page tab).</summary>
        public string? Role { get; set; }
    }
}