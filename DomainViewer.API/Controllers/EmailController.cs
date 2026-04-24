using DomainViewer.API.Common;
using DomainViewer.Core.Entities;
using DomainViewer.Core.Enums;
using DomainViewer.Core.Interfaces;
using DomainViewer.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace DomainViewer.API.Controllers;

[Authorize]
public class EmailController : BaseApiController
{
    private readonly IEmailService _emailService;
    private readonly ApplicationDbContext _context;

    public EmailController(IEmailService emailService, ApplicationDbContext context)
    {
        _emailService = emailService;
        _context = context;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendEmail([FromBody] SendEmailRequest request)
    {
        try
        {
            await _emailService.SendEmailAsync(
                request.ToEmail,
                request.Subject,
                request.Body,
                request.IsHtml);

            return ApiOk(new { to = request.ToEmail }, "ส่งอีเมลสำเร็จ");
        }
        catch (Exception ex)
        {
            return ApiInternalError($"ส่งอีเมลไม่สำเร็จ: {ex.Message}");
        }
    }

    [HttpPost("test")]
    public async Task<IActionResult> SendTestEmail()
    {
        var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(userEmail))
            return ApiUnauthorized("ไม่พบอีเมลผู้ใช้");

        try
        {
            var body = @"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <div style='background: #f3f4f6; padding: 20px; border-radius: 8px; text-align: center;'>
                        <h1 style='color: #111827; margin-bottom: 8px;'>ทดสอบส่งอีเมลสำเร็จ!</h1>
                        <p style='color: #6b7280;'>ระบบ Domain Viewer สามารถส่งอีเมลได้ปกติ</p>
                    </div>
                    <div style='margin-top: 20px; padding: 16px; background: #fef3c7; border-radius: 8px; border-left: 4px solid #f59e0b;'>
                        <p style='margin: 0; color: #92400e;'><strong>เวลาที่ส่ง:</strong> " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + @"</p>
                        <p style='margin: 8px 0 0 0; color: #92400e;'><strong>ผู้ส่ง:</strong> Domain Viewer Notification</p>
                    </div>
                    <hr style='margin: 24px 0; border: none; border-top: 1px solid #e5e7eb;'>
                    <p style='color: #9ca3af; font-size: 12px; text-align: center;'>ข้อความนี้ส่งอัตโนมัติจากระบบ Domain Viewer</p>
                </body>
                </html>";

            await _emailService.SendEmailAsync(
                userEmail,
                "[ทดสอบ] ระบบแจ้งเตือน Domain Viewer",
                body,
                isHtml: true);

            return ApiOk(new { to = userEmail }, "ส่งอีเมลทดสอบสำเร็จ");
        }
        catch (Exception ex)
        {
            return ApiInternalError($"ส่งอีเมลทดสอบไม่สำเร็จ: {ex.Message}");
        }
    }

    [HttpPost("send-domain-list")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> SendDomainListReport([FromBody] SendDomainListReportRequest request)
    {
        var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
        if (currentUserRole != UserRole.Owner.ToString())
            return ApiForbidden();

        if (request.ToEmails == null || request.ToEmails.Length == 0)
            return ApiBadRequest("กรุณาระบุผู้รับอีเมลอย่างน้อย 1 คน", ErrorCodes.ValidationFailed);

        var template = await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Type == EmailTemplateType.DomainListReport && t.IsEnabled);

        if (template == null)
            return ApiBadRequest("ไม่พบเทมเพลตรายงานหรือเทมเพลตถูกปิดใช้งาน", ErrorCodes.NotFound);

        var domains = await _context.Domains
            .Where(d => d.IsActive)
            .OrderBy(d => d.ExpirationDate)
            .ToListAsync();

        var tableHtml = BuildDomainTableHtml(domains);
        var subject = template.Subject;
        var body = template.Body.Replace("{DomainTable}", tableHtml);

        var sentCount = 0;
        var failedEmails = new List<string>();

        foreach (var email in request.ToEmails)
        {
            try
            {
                await _emailService.SendEmailAsync(email.Trim(), subject, body, isHtml: true);
                sentCount++;
            }
            catch (Exception ex)
            {
                failedEmails.Add($"{email}: {ex.Message}");
            }
        }

        if (failedEmails.Count > 0)
        {
            return ApiOk(new { sentCount, failedCount = failedEmails.Count, failures = failedEmails },
                $"ส่งสำเร็จ {sentCount} จาก {request.ToEmails.Length} อีเมล");
        }

        return ApiOk(new { sentCount }, "ส่งรายงานสำเร็จ");
    }

    private static string BuildDomainTableHtml(List<Domain> domains)
    {
        if (domains.Count == 0)
            return "<p>ไม่มี Domain ในระบบ</p>";

        var sb = new StringBuilder();
        sb.Append("<table style='border-collapse: collapse; width: 100%; margin-top: 12px;'>");
        sb.Append("<thead><tr style='background: #f3f4f6;'>");
        sb.Append("<th style='border: 1px solid #d1d5db; padding: 8px; text-align: left;'>Domain</th>");
        sb.Append("<th style='border: 1px solid #d1d5db; padding: 8px; text-align: left;'>วันหมดอายุ</th>");
        sb.Append("<th style='border: 1px solid #d1d5db; padding: 8px; text-align: left;'>เหลือวัน</th>");
        sb.Append("</tr></thead><tbody>");

        foreach (var domain in domains)
        {
            var daysUntil = (domain.ExpirationDate.Date - DateTime.UtcNow.Date).Days;
            var color = daysUntil <= 7 ? "#dc2626" : daysUntil <= 30 ? "#ea580c" : "#16a34a";
            sb.Append("<tr>");
            sb.Append($"<td style='border: 1px solid #d1d5db; padding: 8px;'>{domain.Name}</td>");
            sb.Append($"<td style='border: 1px solid #d1d5db; padding: 8px;'>{domain.ExpirationDate:dd/MM/yyyy}</td>");
            sb.Append($"<td style='border: 1px solid #d1d5db; padding: 8px; color: {color}; font-weight: bold;'>{daysUntil} วัน</td>");
            sb.Append("</tr>");
        }

        sb.Append("</tbody></table>");
        return sb.ToString();
    }
}

public class SendEmailRequest
{
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
}

public class SendDomainListReportRequest
{
    public string[] ToEmails { get; set; } = Array.Empty<string>();
}
