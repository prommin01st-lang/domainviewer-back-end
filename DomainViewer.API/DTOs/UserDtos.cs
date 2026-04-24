using DomainViewer.Core.Enums;

namespace DomainViewer.API.DTOs;

public record CreateUserRequest(
    string Email,
    string Name,
    UserRole Role
);

public record CreateUserByOwnerRequest(
    string Email,
    string Name,
    string Password,
    UserRole Role = UserRole.Employee
);

public record UserResponse(
    Guid Id,
    string Email,
    string Name,
    string Provider,
    string? AvatarUrl,
    string? AvatarBgColor,
    UserRole Role,
    bool IsActive,
    bool IsBanned,
    DateTime CreatedAt
);

public record SyncUserRequest(
    string Email,
    string Name,
    string Provider,
    string ExternalId,
    string? AvatarUrl
);

public record SyncUserResponse(
    Guid Id,
    string Email,
    string Name,
    UserRole Role,
    bool IsNewUser
);
