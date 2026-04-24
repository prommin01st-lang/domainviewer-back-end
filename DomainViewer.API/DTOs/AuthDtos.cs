using DomainViewer.Core.Enums;

namespace DomainViewer.API.DTOs;

public record LoginRequest(
    string Email,
    string Password
);

public record RegisterRequest(
    string Email,
    string Name,
    string Password,
    UserRole Role = UserRole.Employee
);

public record RefreshTokenRequest(
    string RefreshToken
);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    UserResponse User
);

public record UpdateProfileRequest(
    string Name,
    string? AvatarUrl,
    string? AvatarBgColor
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);
