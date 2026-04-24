namespace DomainViewer.API.DTOs;

public record AlertSettingsResponse(
    int Id,
    int? AlertMonths,
    int? AlertWeeks,
    int? AlertDays,
    bool IsEnabled,
    DateTime UpdatedAt
);

public record UpdateAlertSettingsRequest(
    int? AlertMonths,
    int? AlertWeeks,
    int? AlertDays,
    bool IsEnabled
);
