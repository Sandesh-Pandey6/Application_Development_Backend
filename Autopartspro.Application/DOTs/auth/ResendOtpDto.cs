namespace Autopartspro.Application.DOTs.auth
{
    public class ResendOtpDto
    {
        public string Email { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
    }
}