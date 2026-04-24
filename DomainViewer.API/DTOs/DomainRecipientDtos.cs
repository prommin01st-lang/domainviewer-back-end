namespace DomainViewer.API.DTOs;

public record DomainRecipientResponse(
    Guid Id,
    Guid UserId,
    string UserName,
    string UserEmail,
    DateTime CreatedAt
);

public record UpdateRecipientsRequest(
    List<Guid> UserIds
);
