using DomainViewer.API.Common;
using DomainViewer.API.DTOs;
using DomainViewer.Core.Entities;
using DomainViewer.Core.Enums;
using DomainViewer.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DomainViewer.API.Controllers;

[Authorize]
public class UsersController : BaseApiController
{
    private readonly ApplicationDbContext _context;

    public UsersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] UserRole? role = null,
        [FromQuery] bool? isActive = null)
    {
        pageSize = Math.Min(pageSize, 100);

        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(u =>
                u.Name.ToLower().Contains(keyword) ||
                u.Email.ToLower().Contains(keyword));
        }

        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);
        else
            query = query.Where(u => u.IsActive);

        var projected = query
            .OrderBy(u => u.Name)
            .Select(u => new UserResponse(
                u.Id,
                u.Email,
                u.Name,
                u.Provider,
                u.AvatarUrl,
                u.AvatarBgColor,
                u.Role,
                u.IsActive,
                u.IsBanned,
                u.CreatedAt
            ));

        var pagedList = await PagedList<UserResponse>.CreateAsync(projected, page, pageSize);
        return ApiOk(pagedList);
    }

    [HttpPost]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserByOwnerRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return ApiBadRequest(errors, ErrorCodes.ValidationFailed);
        }

        var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
        if (currentUserRole != UserRole.Owner.ToString())
            return ApiForbidden();

        if (await _context.Users.AnyAsync(u => u.Email == request.Email.Trim().ToLower()))
            return ApiConflict("Email นี้มีอยู่ในระบบแล้ว", ErrorCodes.Duplicate);

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Email = request.Email.Trim().ToLower(),
            Name = request.Name.Trim(),
            Role = request.Role,
            Provider = "local",
            ExternalId = Guid.NewGuid().ToString(),
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
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
        ), "สร้างผู้ใช้สำเร็จ");
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
        if (currentUserRole != UserRole.Owner.ToString())
            return ApiForbidden();

        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return ApiNotFound("ไม่พบผู้ใช้");

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (user.Id.ToString() == currentUserId)
            return ApiBadRequest("ไม่สามารถลบตัวเองได้", ErrorCodes.InvalidOperation);

        if (user.Role == UserRole.Owner)
        {
            var ownerCount = await _context.Users.CountAsync(u => u.Role == UserRole.Owner && u.IsActive);
            if (ownerCount <= 1)
                return ApiBadRequest("ไม่สามารถลบเจ้าของคนสุดท้ายได้", ErrorCodes.InvalidOperation);
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ApiOk("ลบผู้ใช้สำเร็จ");
    }

    [HttpPut("{id:guid}/ban")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> ToggleBanUser(Guid id)
    {
        var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
        if (currentUserRole != UserRole.Owner.ToString())
            return ApiForbidden();

        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return ApiNotFound("ไม่พบผู้ใช้");

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (user.Id.ToString() == currentUserId)
            return ApiBadRequest("ไม่สามารถล็อคตัวเองได้", ErrorCodes.InvalidOperation);

        user.IsBanned = !user.IsBanned;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var status = user.IsBanned ? "ล็อค" : "ปลดล็อค";
        return ApiOk($"{status}ผู้ใช้สำเร็จ");
    }
}
