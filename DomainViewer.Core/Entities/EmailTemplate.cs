using DomainViewer.Core.Enums;

namespace DomainViewer.Core.Entities;

public class EmailTemplate
{
    public int Id { get; set; }
    public EmailTemplateType Type { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
