using DomainViewer.API.Common;
using DomainViewer.Core.Entities;
using DomainViewer.Core.Enums;
using DomainViewer.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DomainViewer.API.Controllers;

[Authorize(Roles = "Owner")]
public class EmailTemplatesController : BaseApiController
{
    private readonly ApplicationDbContext _context;

    public EmailTemplatesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetTemplates()
    {
        var templates = await _context.EmailTemplates
            .OrderBy(t => t.Type)
            .Select(t => new EmailTemplateResponse(
                t.Id,
                t.Type.ToString(),
                t.Subject,
                t.Body,
                t.IsEnabled,
                t.UpdatedAt
            ))
            .ToListAsync();

        return ApiOk(templates);
    }

    [HttpPut("{type}")]
    public async Task<IActionResult> UpdateTemplate(string type, [FromBody] UpdateEmailTemplateRequest request)
    {
        if (!Enum.TryParse<EmailTemplateType>(type, true, out var templateType))
            return ApiBadRequest("Invalid template type", ErrorCodes.ValidationFailed);

        var template = await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Type == templateType);

        if (template == null)
            return ApiNotFound("ไม่พบเทมเพลต");

        template.Subject = request.Subject.Trim();
        template.Body = request.Body.Trim();
        template.IsEnabled = request.IsEnabled;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return ApiOk(new EmailTemplateResponse(
            template.Id,
            template.Type.ToString(),
            template.Subject,
            template.Body,
            template.IsEnabled,
            template.UpdatedAt
        ), "บันทึกเทมเพลตสำเร็จ");
    }
}

public record EmailTemplateResponse(
    int Id,
    string Type,
    string Subject,
    string Body,
    bool IsEnabled,
    DateTime UpdatedAt
);

public class UpdateEmailTemplateRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}
