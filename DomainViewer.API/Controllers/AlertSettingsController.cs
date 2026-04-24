using DomainViewer.API.Common;
using DomainViewer.API.DTOs;
using DomainViewer.Core.Enums;
using DomainViewer.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DomainViewer.API.Controllers;

[Authorize]
public class AlertSettingsController : BaseApiController
{
    private readonly ApplicationDbContext _context;

    public AlertSettingsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _context.GlobalAlertSettings.FirstOrDefaultAsync();
        if (settings == null)
            return ApiNotFound("Settings not found");

        return ApiOk(new AlertSettingsResponse(
            settings.Id,
            settings.AlertMonths,
            settings.AlertWeeks,
            settings.AlertDays,
            settings.IsEnabled,
            settings.UpdatedAt
        ));
    }

    [HttpPut]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateAlertSettingsRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return ApiBadRequest(errors, ErrorCodes.ValidationFailed);
        }

        var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
        if (currentUserRole != UserRole.Owner.ToString())
            return ApiForbidden();

        var settings = await _context.GlobalAlertSettings.FirstOrDefaultAsync();
        if (settings == null)
            return ApiNotFound("Settings not found");

        settings.AlertMonths = request.AlertMonths;
        settings.AlertWeeks = request.AlertWeeks;
        settings.AlertDays = request.AlertDays;
        settings.IsEnabled = request.IsEnabled;
        settings.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return ApiOk(new AlertSettingsResponse(
            settings.Id,
            settings.AlertMonths,
            settings.AlertWeeks,
            settings.AlertDays,
            settings.IsEnabled,
            settings.UpdatedAt
        ), "บันทึกการตั้งค่าสำเร็จ");
    }
}
