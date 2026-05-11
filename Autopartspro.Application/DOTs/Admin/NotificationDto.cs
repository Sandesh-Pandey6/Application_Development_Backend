namespace Autopartspro.Application.DOTs.admin
{
    public class NotificationResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationListDto
    {
        public int TotalUnread { get; set; }
        public int LowStockCount { get; set; }
        public int CreditReminderCount { get; set; }
        public int InfoCount { get; set; }
        public List<NotificationResponseDto> Notifications { get; set; } = new();
    }
}