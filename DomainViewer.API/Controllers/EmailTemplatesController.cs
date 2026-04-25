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

    [HttpPost("{type}/reset")]
    public async Task<IActionResult> ResetTemplate(string type)
    {
        if (!Enum.TryParse<EmailTemplateType>(type, true, out var templateType))
            return ApiBadRequest("Invalid template type", ErrorCodes.ValidationFailed);

        var template = await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Type == templateType);

        if (template == null)
            return ApiNotFound("ไม่พบเทมเพลต");

        var defaults = new Dictionary<EmailTemplateType, (string Subject, string Body)>
        {
            [EmailTemplateType.ExpirationAlert] = (
                "[แจ้งเตือน] Domain {DomainName} จะหมดอายุในอีก {DaysUntilExpiration} วัน",
                @"<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6;'>
    <h2 style='color: #dc2626;'>⚠️ แจ้งเตือนวันหมดอายุ Domain</h2>
    <p><strong>Domain:</strong> {DomainName}</p>
    <p><strong>วันหมดอายุ:</strong> {ExpirationDate}</p>
    <p><strong>เหลือเวลา:</strong> <span style='color: #dc2626; font-size: 18px;'>{DaysUntilExpiration} วัน</span></p>
    <hr style='margin: 20px 0;'>
    <p style='color: #666; font-size: 12px;'>ข้อความนี้ส่งอัตโนมัติจากระบบ Domain Viewer</p>
</body>
</html>"
            ),
            [EmailTemplateType.DomainListReport] = (
                "[รายงาน] รายการ Domain ทั้งหมดในระบบ",
                @"<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6;'>
    <h2 style='color: #2563eb;'>📋 รายงานรายการ Domain</h2>
    <p>รายการ Domain ทั้งหมดในระบบ Domain Viewer มีดังนี้:</p>
    {DomainTable}
    <hr style='margin: 20px 0;'>
    <p style='color: #666; font-size: 12px;'>ข้อความนี้ส่งอัตโนมัติจากระบบ Domain Viewer</p>
</body>
</html>"
            ),
            [EmailTemplateType.ExpiredAlert] = (
                "[แจ้งเตือน] Domain {DomainName} หมดอายุแล้ว",
                @"<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6;'>
    <h2 style='color: #dc2626;'>🚨 Domain หมดอายุแล้ว</h2>
    <p><strong>Domain:</strong> {DomainName}</p>
    <p><strong>วันหมดอายุ:</strong> {ExpirationDate}</p>
    <p><strong>สถานะ:</strong> <span style='color: #dc2626; font-size: 18px;'>หมดอายุแล้ว ({DaysUntilExpiration} วัน)</span></p>
    <hr style='margin: 20px 0;'>
    <p style='color: #666; font-size: 12px;'>ข้อความนี้ส่งอัตโนมัติจากระบบ Domain Viewer</p>
</body>
</html>"
            )
        };

        if (!defaults.TryGetValue(templateType, out var defaultValues))
            return ApiBadRequest("ไม่พบค่าเริ่มต้นสำหรับเทมเพลตนี้", ErrorCodes.ValidationFailed);

        template.Subject = defaultValues.Subject;
        template.Body = defaultValues.Body;
        template.IsEnabled = true;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return ApiOk(new EmailTemplateResponse(
            template.Id,
            template.Type.ToString(),
            template.Subject,
            template.Body,
            template.IsEnabled,
            template.UpdatedAt
        ), "คืนค่าเทมเพลตเริ่มต้นสำเร็จ");
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
