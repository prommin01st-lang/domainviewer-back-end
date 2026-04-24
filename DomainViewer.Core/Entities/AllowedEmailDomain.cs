namespace DomainViewer.Core.Entities;

public class AllowedEmailDomain
{
    public int Id { get; set; }
    public string Domain { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
