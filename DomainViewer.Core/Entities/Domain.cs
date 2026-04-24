namespace DomainViewer.Core.Entities;

public class Domain
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? RegistrationDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public string? Registrant { get; set; }
    public string? Registrar { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User? Creator { get; set; }
    public ICollection<DomainNotificationRecipient> DomainNotificationRecipients { get; set; } = new List<DomainNotificationRecipient>();
    public ICollection<NotificationLog> NotificationLogs { get; set; } = new List<NotificationLog>();
    public ICollection<DomainAttachment> DomainAttachments { get; set; } = new List<DomainAttachment>();
}
