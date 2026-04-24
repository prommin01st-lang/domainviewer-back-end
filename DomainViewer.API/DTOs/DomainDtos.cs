namespace DomainViewer.API.DTOs;

public record CreateDomainRequest(
    string Name,
    string? Description,
    DateTime? RegistrationDate,
    DateTime ExpirationDate,
    string? Registrant,
    string? Registrar,
    string? ImageUrl
);

public record UpdateDomainRequest(
    string Name,
    string? Description,
    DateTime? RegistrationDate,
    DateTime ExpirationDate,
    string? Registrant,
    string? Registrar,
    string? ImageUrl,
    bool IsActive
);

public record DomainResponse(
    Guid Id,
    string Name,
    string? Description,
    DateTime? RegistrationDate,
    DateTime ExpirationDate,
    string? Registrant,
    string? Registrar,
    string? ImageUrl,
    bool IsActive,
    Guid? CreatedBy,
    string? CreatorName,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int DaysUntilExpiration
);
