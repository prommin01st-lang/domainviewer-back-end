using DomainViewer.API.Common;
using DomainViewer.API.DTOs;
using DomainViewer.Core.Entities;
using DomainViewer.Core.Enums;
using DomainViewer.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DomainViewer.API.Controllers;

public class AuthController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return ApiBadRequest(errors, ErrorCodes.ValidationFailed);
        }

        var expectedKey = _configuration["Registration:SecretKey"];
        if (!string.IsNullOrWhiteSpace(expectedKey) && request.SecretKey != expectedKey)
            return ApiBadRequest("Secret key ไม่ถูกต้อง", ErrorCodes.ValidationFailed);

        var email = request.Email.Trim().ToLower();

        if (await _context.Users.AnyAsync(u => u.Email == email))
            return ApiConflict("Email already exists", ErrorCodes.Duplicate);

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Email = email,
            Name = request.Name.Trim(),
            Provider = "credentials",
            ExternalId = Guid.NewGuid().ToString(),
            PasswordHash = passwordHash,
            Role = request.Role,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var (accessToken, accessTokenExpiresAt) = GenerateJwtToken(user);
        var refreshToken = await GenerateRefreshToken(user.Id);

        return ApiOk(new AuthResponse(
            accessToken,
            refreshToken.Token,
            accessTokenExpiresAt,
            new UserResponse(
                user.Id,
                user.Email,
                user.Name,
                user.Provider,
                user.AvatarUrl,
                user.AvatarBgColor,
                user.Role,
                user.IsActive,
                user.IsBanned,
                user.CreatedAt
            )
        ), "Register successful");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return ApiBadRequest(errors, ErrorCodes.ValidationFailed);
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.Trim().ToLower() && u.IsActive);

        if (user == null || user.PasswordHash == null)
            return ApiUnauthorized("Invalid email or password");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return ApiUnauthorized("Invalid email or password");

        if (user.IsBanned)
            return ApiUnauthorized("บัญชีถูกล็อค กรุณาติดต่อผู้ดูแลระบบ", ErrorCodes.Forbidden);

        var (accessToken, accessTokenExpiresAt) = GenerateJwtToken(user);
        var refreshToken = await GenerateRefreshToken(user.Id);

        return ApiOk(new AuthResponse(
            accessToken,
            refreshToken.Token,
            accessTokenExpiresAt,
            new UserResponse(
                user.Id,
                user.Email,
                user.Name,
                user.Provider,
                user.AvatarUrl,
                user.AvatarBgColor,
                user.Role,
                user.IsActive,
                user.IsBanned,
                user.CreatedAt
            )
        ), "Login successful");
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
            return ApiBadRequest("Refresh token is required", ErrorCodes.ValidationFailed);

        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (storedToken == null)
            return ApiUnauthorized("Invalid refresh token");

        if (!storedToken.IsActive)
            return ApiUnauthorized("Refresh token has been revoked or expired");

        var user = storedToken.User;
        if (user == null || !user.IsActive)
            return ApiUnauthorized("User not found or inactive");

        if (user.IsBanned)
            return ApiUnauthorized("บัญชีถูกล็อค กรุณาติดต่อผู้ดูแลระบบ", ErrorCodes.Forbidden);

        storedToken.RevokedAt = DateTime.UtcNow;

        var (accessToken, accessTokenExpiresAt) = GenerateJwtToken(user);
        var newRefreshToken = await GenerateRefreshToken(user.Id);

        storedToken.ReplacedByToken = newRefreshToken.Token;

        await _context.SaveChangesAsync();

        return ApiOk(new AuthResponse(
            accessToken,
            newRefreshToken.Token,
            accessTokenExpiresAt,
            new UserResponse(
                user.Id,
                user.Email,
                user.Name,
                user.Provider,
                user.AvatarUrl,
                user.AvatarBgColor,
                user.Role,
                user.IsActive,
                user.IsBanned,
                user.CreatedAt
            )
        ), "Refresh token successful");
    }

    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
            return ApiBadRequest("Refresh token is required", ErrorCodes.ValidationFailed);

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (storedToken == null)
            return ApiNotFound("Refresh token not found");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (storedToken.UserId.ToString() != userId)
            return ApiForbidden("You can only revoke your own tokens");

        storedToken.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ApiOk("Token revoked successfully");
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var id))
            return ApiUnauthorized();

        var user = await _context.Users.FindAsync(id);
        if (user == null || !user.IsActive)
            return ApiUnauthorized("User not found or inactive");

        return ApiOk(new UserResponse(
            user.Id,
            user.Email,
            user.Name,
            user.Provider,
            user.AvatarUrl,
            user.AvatarBgColor,
            user.Role,
            user.IsActive,
            user.IsBanned,
            user.CreatedAt
        ));
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return ApiBadRequest(errors, ErrorCodes.ValidationFailed);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var id))
            return ApiUnauthorized();

        var user = await _context.Users.FindAsync(id);
        if (user == null || !user.IsActive)
            return ApiUnauthorized("User not found or inactive");

        user.Name = request.Name.Trim();
        if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
            user.AvatarUrl = request.AvatarUrl.Trim();
        if (!string.IsNullOrWhiteSpace(request.AvatarBgColor))
            user.AvatarBgColor = request.AvatarBgColor.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ApiOk(new UserResponse(
            user.Id,
            user.Email,
            user.Name,
            user.Provider,
            user.AvatarUrl,
            user.AvatarBgColor,
            user.Role,
            user.IsActive,
            user.IsBanned,
            user.CreatedAt
        ), "อัปเดตโปรไฟล์สำเร็จ");
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return ApiBadRequest(errors, ErrorCodes.ValidationFailed);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var id))
            return ApiUnauthorized();

        var user = await _context.Users.FindAsync(id);
        if (user == null || !user.IsActive)
            return ApiUnauthorized("User not found or inactive");

        if (user.PasswordHash == null || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return ApiBadRequest("รหัสผ่านปัจจุบันไม่ถูกต้อง", ErrorCodes.ValidationFailed);

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ApiOk("เปลี่ยนรหัสผ่านสำเร็จ");
    }

    private (string token, DateTime expiresAt) GenerateJwtToken(User user)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "DomainViewer";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "DomainViewer";
        var expiryMinutes = int.TryParse(_configuration["Jwt:AccessTokenExpiryMinutes"], out var mins) ? mins : 15;

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private async Task<RefreshToken> GenerateRefreshToken(Guid userId)
    {
        var expiryDays = int.TryParse(_configuration["Jwt:RefreshTokenExpiryDays"], out var days) ? days : 30;

        var refreshToken = new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays)
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return refreshToken;
    }
}
