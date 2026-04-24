using DomainViewer.API.Common;
using DomainViewer.API.DTOs;
using DomainViewer.Core.Entities;
using DomainViewer.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DomainViewer.API.Controllers;

[Authorize(Roles = "Owner")]
public class NotificationLogsController : BaseApiController
{
    private readonly ApplicationDbContext _context;

    public NotificationLogsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] Guid? domainId,
        [FromQuery] NotificationStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        pageSize = Math.Min(pageSize, 100);

        var query = _context.NotificationLogs
            .Include(l => l.Domain)
            .Include(l => l.User)
            .AsQueryable();

        if (domainId.HasValue)
            query = query.Where(l => l.DomainId == domainId.Value);

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        var projected = query
            .OrderByDescending(l => l.SentAt)
            .Select(l => new NotificationLogResponse(
                l.Id,
                l.DomainId,
                l.Domain.Name,
                l.UserId,
                l.User.Email,
                l.AlertType,
                l.SentAt,
                l.Status,
                l.ErrorMessage,
                l.ExpirationDate
            ));

        var pagedList = await PagedList<NotificationLogResponse>.CreateAsync(projected, page, pageSize);
        return ApiOk(pagedList);
    }
}
