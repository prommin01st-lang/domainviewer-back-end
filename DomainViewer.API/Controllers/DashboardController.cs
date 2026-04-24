using DomainViewer.API.Common;
using DomainViewer.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DomainViewer.API.Controllers;

[Authorize]
public class DashboardController : BaseApiController
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var today = DateTime.UtcNow.Date;

        var totalDomains = await _context.Domains.CountAsync(d => d.IsActive);
        var expiredDomains = await _context.Domains.CountAsync(d => d.IsActive && d.ExpirationDate.Date < today);
        var upcomingDomains = await _context.Domains.CountAsync(d => d.IsActive && d.ExpirationDate.Date >= today && d.ExpirationDate.Date <= today.AddDays(30));
        var totalUsers = await _context.Users.CountAsync(u => u.IsActive);

        return ApiOk(new DashboardStatsResponse(
            totalDomains,
            expiredDomains,
            upcomingDomains,
            totalUsers
        ));
    }

    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcomingDomains([FromQuery] int days = 30, [FromQuery] int limit = 5)
    {
        var today = DateTime.UtcNow.Date;
        var targetDate = today.AddDays(days);

        var domains = await _context.Domains
            .Where(d => d.IsActive && d.ExpirationDate.Date >= today && d.ExpirationDate.Date <= targetDate)
            .OrderBy(d => d.ExpirationDate)
            .Take(limit)
            .Select(d => new UpcomingDomainResponse(
                d.Id,
                d.Name,
                d.ExpirationDate,
                (d.ExpirationDate.Date - today).Days
            ))
            .ToListAsync();

        return ApiOk(domains);
    }
}

public record DashboardStatsResponse(
    int TotalDomains,
    int ExpiredDomains,
    int UpcomingDomains,
    int TotalUsers
);

public record UpcomingDomainResponse(
    Guid Id,
    string Name,
    DateTime ExpirationDate,
    int DaysUntilExpiration
);
