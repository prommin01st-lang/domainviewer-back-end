namespace DomainViewer.Core.Entities;

public class GlobalAlertSetting
{
    public int Id { get; set; }
    public int? AlertMonths { get; set; }
    public int? AlertWeeks { get; set; }
    public int? AlertDays { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
