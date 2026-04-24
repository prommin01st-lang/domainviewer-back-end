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

[Route("api/domains/{domainId:guid}/[controller]")]
[Authorize]
public class RecipientsController : BaseApiController
{
    private readonly ApplicationDbContext _context;

    public RecipientsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecipients(Guid domainId)
    {
        var domainExists = await _context.Domains.AnyAsync(d => d.Id == domainId);
        if (!domainExists)
            return ApiNotFound("Domain not found");

        var recipients = await _context.DomainNotificationRecipients
            .Where(r => r.DomainId == domainId)
            .Include(r => r.User)
            .Select(r => new DomainRecipientResponse(
                r.Id,
                r.UserId,
                r.User.Name,
                r.User.Email,
                r.CreatedAt
            ))
            .ToListAsync();

        return ApiOk(recipients);
    }

    [HttpPut]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> UpdateRecipients(Guid domainId, [FromBody] UpdateRecipientsRequest request)
    {
        var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
        if (currentUserRole != UserRole.Owner.ToString())
            return ApiForbidden();

        var domainExists = await _context.Domains.AnyAsync(d => d.Id == domainId);
        if (!domainExists)
            return ApiNotFound("Domain not found");

        var validUserIds = await _context.Users
            .Where(u => request.UserIds.Contains(u.Id) && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync();

        var invalidIds = request.UserIds.Except(validUserIds).ToList();
        if (invalidIds.Any())
            return ApiBadRequest($"Invalid user IDs: {string.Join(", ", invalidIds)}", ErrorCodes.ValidationFailed);

        var existing = await _context.DomainNotificationRecipients
            .Where(r => r.DomainId == domainId)
            .ToListAsync();
        _context.DomainNotificationRecipients.RemoveRange(existing);

        foreach (var userId in request.UserIds)
        {
            _context.DomainNotificationRecipients.Add(new DomainNotificationRecipient
            {
                DomainId = domainId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return ApiOk("อัปเดตผู้รับแจ้งเตือนสำเร็จ");
    }
}
