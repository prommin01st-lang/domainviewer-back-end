using DomainViewer.API.Common;
using DomainViewer.API.DTOs;
using DomainViewer.Core.Entities;
using DomainViewer.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DomainViewer.API.Controllers;

[Authorize(Roles = "Owner")]
public class AllowedEmailDomainsController : BaseApiController
{
    private readonly ApplicationDbContext _context;

    public AllowedEmailDomainsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllowedDomains(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Min(pageSize, 100);

        var projected = _context.AllowedEmailDomains
            .OrderBy(a => a.Domain)
            .Select(a => new AllowedEmailDomainResponse(
                a.Id,
                a.Domain,
                a.CreatedAt
            ));

        var pagedList = await PagedList<AllowedEmailDomainResponse>.CreateAsync(projected, page, pageSize);
        return ApiOk(pagedList);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAllowedDomain([FromBody] CreateAllowedEmailDomainRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return ApiBadRequest(errors, ErrorCodes.ValidationFailed);
        }

        var domain = request.Domain.Trim().ToLower();

        if (await _context.AllowedEmailDomains.AnyAsync(a => a.Domain == domain))
            return ApiConflict("Domain already exists", ErrorCodes.Duplicate);

        var allowedDomain = new AllowedEmailDomain
        {
            Domain = domain,
            CreatedAt = DateTime.UtcNow
        };

        _context.AllowedEmailDomains.Add(allowedDomain);
        await _context.SaveChangesAsync();

        return ApiOk(new AllowedEmailDomainResponse(allowedDomain.Id, allowedDomain.Domain, allowedDomain.CreatedAt), "เพิ่มโดเมนสำเร็จ");
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAllowedDomain(int id)
    {
        var domain = await _context.AllowedEmailDomains.FindAsync(id);
        if (domain == null)
            return ApiNotFound("Domain not found");

        _context.AllowedEmailDomains.Remove(domain);
        await _context.SaveChangesAsync();

        return ApiOk("ลบโดเมนสำเร็จ");
    }
}
