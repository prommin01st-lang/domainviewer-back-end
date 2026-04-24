namespace DomainViewer.Core.Entities;

public enum NotificationStatus : byte
{
    Pending = 0,
    Sent = 1,
    Failed = 2
}

public class NotificationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DomainId { get; set; }
    public Guid UserId { get; set; }
    public string AlertType { get; set; } = string.Empty; // Month, Week, Day
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime ExpirationDate { get; set; }
    public int RetryCount { get; set; } = 0;
    public DateTime? NextRetryAt { get; set; }

    // Navigation properties
    public Domain Domain { get; set; } = null!;
    public User User { get; set; } = null!;
}
