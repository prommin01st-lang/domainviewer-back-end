using DomainViewer.Core.Entities;
using DomainViewer.Core.Interfaces;
using DomainViewer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace DomainViewer.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public class DomainExpirationNotificationJob : IJob
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public DomainExpirationNotificationJob(
        ApplicationDbContext context,
        IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    private async Task<(string Subject, string Body)?> GetEmailTemplateAsync(Core.Enums.EmailTemplateType templateType, string domainName, DateTime expirationDate, int daysUntilExpiration)
    {
        var template = await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Type == templateType && t.IsEnabled);

        if (template == null)
            return null;

        var subject = template.Subject
            .Replace("{DomainName}", domainName)
            .Replace("{ExpirationDate}", expirationDate.ToString("dd/MM/yyyy"))
            .Replace("{DaysUntilExpiration}", daysUntilExpiration.ToString());

        var body = template.Body
            .Replace("{DomainName}", domainName)
            .Replace("{ExpirationDate}", expirationDate.ToString("dd/MM/yyyy"))
            .Replace("{DaysUntilExpiration}", daysUntilExpiration.ToString());

        return (subject, body);
    }

    public async Task Execute(IJobExecutionContext context)
    {
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Starting domain expiration notification job");

        var globalSettings = await _context.GlobalAlertSettings.FirstOrDefaultAsync();
        if (globalSettings == null || !globalSettings.IsEnabled)
        {
            Console.WriteLine("Global alert settings not found or disabled. Skipping notification.");
            return;
        }

        var today = DateTime.UtcNow.Date;
        var alertThresholds = new List<int>();

        if (globalSettings.AlertMonths.HasValue && globalSettings.AlertMonths.Value > 0)
            alertThresholds.Add(globalSettings.AlertMonths.Value * 30);
        if (globalSettings.AlertWeeks.HasValue && globalSettings.AlertWeeks.Value > 0)
            alertThresholds.Add(globalSettings.AlertWeeks.Value * 7);
        if (globalSettings.AlertDays.HasValue && globalSettings.AlertDays.Value > 0)
            alertThresholds.Add(globalSettings.AlertDays.Value);

        if (alertThresholds.Count == 0)
        {
            Console.WriteLine("No alert thresholds configured. Skipping notification.");
            return;
        }

        await ProcessScheduledAlerts(today, alertThresholds);
        await ProcessExpiredAlerts(today);
        await ProcessRetryAlerts(today);

        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Domain expiration notification job completed");
    }

    private async Task ProcessScheduledAlerts(DateTime today, List<int> alertThresholds)
    {
        var domains = await _context.Domains
            .Where(d => d.IsActive && d.ExpirationDate > today)
            .Include(d => d.DomainNotificationRecipients)
                .ThenInclude(r => r.User)
            .ToListAsync();

        foreach (var domain in domains)
        {
            var daysUntilExpiration = (domain.ExpirationDate.Date - today).Days;

            if (!alertThresholds.Contains(daysUntilExpiration))
                continue;

            var recipients = domain.DomainNotificationRecipients
                .Select(r => r.User)
                .Where(u => u.IsActive)
                .ToList();

            if (recipients.Count == 0)
            {
                Console.WriteLine($"Warning: No recipients configured for domain {domain.Name}");
                continue;
            }

            var templateResult = await GetEmailTemplateAsync(Core.Enums.EmailTemplateType.ExpirationAlert, domain.Name, domain.ExpirationDate, daysUntilExpiration);
            if (templateResult == null)
            {
                Console.WriteLine($"Skipping notification for {domain.Name}: email template not found or disabled");
                continue;
            }
            var (subject, body) = templateResult.Value;
            var alertType = $"{daysUntilExpiration}d";

            foreach (var user in recipients)
            {
                await TrySendAndLogAsync(
                    domain.Id,
                    user.Id,
                    user.Email,
                    domain.Name,
                    subject,
                    body,
                    alertType,
                    domain.ExpirationDate);
            }
        }
    }

    private async Task ProcessExpiredAlerts(DateTime today)
    {
        var expiredDomains = await _context.Domains
            .Where(d => d.IsActive && d.ExpirationDate.Date < today)
            .Include(d => d.DomainNotificationRecipients)
                .ThenInclude(r => r.User)
            .ToListAsync();

        foreach (var domain in expiredDomains)
        {
            var daysUntilExpiration = (domain.ExpirationDate.Date - today).Days;
            var recipients = domain.DomainNotificationRecipients
                .Select(r => r.User)
                .Where(u => u.IsActive)
                .ToList();

            if (recipients.Count == 0)
            {
                Console.WriteLine($"Warning: No recipients configured for expired domain {domain.Name}");
                continue;
            }

            var templateResult = await GetEmailTemplateAsync(Core.Enums.EmailTemplateType.ExpiredAlert, domain.Name, domain.ExpirationDate, daysUntilExpiration);
            if (templateResult == null)
            {
                Console.WriteLine($"Skipping expired notification for {domain.Name}: email template not found or disabled");
                continue;
            }
            var (subject, body) = templateResult.Value;
            var alertType = "expired";

            foreach (var user in recipients)
            {
                await TrySendAndLogAsync(
                    domain.Id,
                    user.Id,
                    user.Email,
                    domain.Name,
                    subject,
                    body,
                    alertType,
                    domain.ExpirationDate);
            }
        }
    }

    private async Task ProcessRetryAlerts(DateTime today)
    {
        var failedLogs = await _context.NotificationLogs
            .Include(l => l.Domain)
            .Include(l => l.User)
            .Where(l => l.Status == NotificationStatus.Failed
                && l.RetryCount < 3
                && (l.NextRetryAt == null || l.NextRetryAt <= DateTime.UtcNow))
            .ToListAsync();

        foreach (var log in failedLogs)
        {
            if (log.Domain == null || !log.Domain.IsActive || log.Domain.ExpirationDate <= today)
            {
                Console.WriteLine($"Skipping retry for log {log.Id}: domain inactive or expired");
                continue;
            }

            if (log.User == null || !log.User.IsActive)
            {
                Console.WriteLine($"Skipping retry for log {log.Id}: user inactive");
                continue;
            }

            var daysUntilExpiration = (log.Domain.ExpirationDate.Date - today).Days;
            var templateResult = await GetEmailTemplateAsync(Core.Enums.EmailTemplateType.ExpirationAlert, log.Domain.Name, log.Domain.ExpirationDate, daysUntilExpiration);
            if (templateResult == null)
            {
                Console.WriteLine($"Skipping retry for log {log.Id}: email template not found or disabled");
                continue;
            }
            var (subject, body) = templateResult.Value;

            try
            {
                await _emailService.SendEmailAsync(log.User.Email, subject, body);
                Console.WriteLine($"Retry success for {log.User.Email} / {log.Domain.Name}");

                log.Status = NotificationStatus.Sent;
                log.ErrorMessage = null;
                log.SentAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                log.RetryCount++;
                var backoffDays = log.RetryCount == 1 ? 1 : log.RetryCount == 2 ? 2 : 4;
                log.NextRetryAt = DateTime.UtcNow.Date.AddDays(backoffDays);
                log.ErrorMessage = ex.Message;
                log.SentAt = DateTime.UtcNow;

                Console.WriteLine($"Retry failed for {log.User.Email} / {log.Domain.Name} (attempt {log.RetryCount}): {ex.Message}");
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save retry log: {ex.Message}");
            }
        }
    }

    private async Task TrySendAndLogAsync(
        Guid domainId,
        Guid userId,
        string userEmail,
        string domainName,
        string subject,
        string body,
        string alertType,
        DateTime expirationDate)
    {
        var alreadySent = await _context.NotificationLogs.AnyAsync(l =>
            l.DomainId == domainId
            && l.UserId == userId
            && l.AlertType == alertType
            && l.ExpirationDate == expirationDate);

        if (alreadySent)
        {
            Console.WriteLine($"Skipping duplicate notification to {userEmail} for {domainName} ({alertType})");
            return;
        }

        var log = new NotificationLog
        {
            DomainId = domainId,
            UserId = userId,
            AlertType = alertType,
            SentAt = DateTime.UtcNow,
            ExpirationDate = expirationDate
        };

        try
        {
            await _emailService.SendEmailAsync(userEmail, subject, body);
            Console.WriteLine($"Sent notification to {userEmail} for domain {domainName}");
            log.Status = NotificationStatus.Sent;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send notification to {userEmail} for domain {domainName}: {ex.Message}");
            log.Status = NotificationStatus.Failed;
            log.ErrorMessage = ex.Message;
            log.NextRetryAt = DateTime.UtcNow.Date.AddDays(1);
        }

        try
        {
            _context.NotificationLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save notification log for {userEmail} / {domainName}: {ex.Message}");
            // Detach the log entry so it doesn't interfere with subsequent saves
            _context.Entry(log).State = EntityState.Detached;
        }
    }


}
