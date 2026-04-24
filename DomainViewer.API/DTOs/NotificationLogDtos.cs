using DomainViewer.Core.Entities;

namespace DomainViewer.API.DTOs;

public record NotificationLogResponse(
    Guid Id,
    Guid DomainId,
    string DomainName,
    Guid UserId,
    string UserEmail,
    string AlertType,
    DateTime SentAt,
    NotificationStatus Status,
    string? ErrorMessage,
    DateTime ExpirationDate
);
