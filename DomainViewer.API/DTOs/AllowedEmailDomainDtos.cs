namespace DomainViewer.API.DTOs;

public record AllowedEmailDomainResponse(
    int Id,
    string Domain,
    DateTime CreatedAt
);

public record CreateAllowedEmailDomainRequest(
    string Domain
);
