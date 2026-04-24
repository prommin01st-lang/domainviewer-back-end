using DomainViewer.Core.Enums;

namespace DomainViewer.Core.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = "google";
    public string ExternalId { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? AvatarBgColor { get; set; }
    public string? PasswordHash { get; set; }
    public UserRole Role { get; set; } = UserRole.Employee;
    public bool IsActive { get; set; } = true;
    public bool IsBanned { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Domain> CreatedDomains { get; set; } = new List<Domain>();
    public ICollection<DomainNotificationRecipient> DomainNotificationRecipients { get; set; } = new List<DomainNotificationRecipient>();
    public ICollection<NotificationLog> NotificationLogs { get; set; } = new List<NotificationLog>();
}
