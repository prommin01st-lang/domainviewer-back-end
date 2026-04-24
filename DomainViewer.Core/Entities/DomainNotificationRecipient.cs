namespace DomainViewer.Core.Entities;

public class DomainNotificationRecipient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DomainId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Domain Domain { get; set; } = null!;
    public User User { get; set; } = null!;
}
