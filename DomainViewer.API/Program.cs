using DomainViewer.API.Common;
using DomainViewer.Core.Entities;
using DomainViewer.Core.Interfaces;
using DomainViewer.Infrastructure.Data;
using DomainViewer.Infrastructure.Jobs;
using DomainViewer.Infrastructure.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Forwarded headers for reverse proxy (ngrok, etc.)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add services to the container.
builder.Services.AddControllers();

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// PostgreSQL DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "DomainViewer";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "DomainViewer";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Email Service
builder.Services.AddScoped<IEmailService, EmailService>();

// Quartz.NET - Background Jobs
builder.Services.AddQuartz(q =>
{
    // Register the job
    var jobKey = new JobKey("DomainExpirationNotificationJob");
    q.AddJob<DomainExpirationNotificationJob>(jobKey, j => j.WithIdentity(jobKey));

    // Trigger: Run every day at midnight (00:00)
    q.AddTrigger(trigger => trigger
        .ForJob(jobKey)
        .WithIdentity("DomainExpirationNotificationTrigger")
        .WithCronSchedule("0 0 0 * * ?", cron => cron.InTimeZone(TimeZoneInfo.Local))
        .WithDescription("Run domain expiration notification daily at midnight"));
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000"];

Console.WriteLine($"[CORS] Allowed Origins: {string.Join(", ", allowedOrigins)}");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<CorsDebugMiddleware>();
app.UseForwardedHeaders();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Auto migrate database and seed default data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    // Seed default alert settings
    if (!db.GlobalAlertSettings.Any())
    {
        db.GlobalAlertSettings.Add(new GlobalAlertSetting
        {
            AlertMonths = 1,
            AlertWeeks = 1,
            AlertDays = 3,
            IsEnabled = true,
            UpdatedAt = DateTime.UtcNow
        });
    }

    // Seed default allowed email domain
    if (!db.AllowedEmailDomains.Any())
    {
        db.AllowedEmailDomains.Add(new AllowedEmailDomain
        {
            Domain = "company.com",
            CreatedAt = DateTime.UtcNow
        });
    }

    // Seed default email templates
    if (!db.EmailTemplates.Any())
    {
        db.EmailTemplates.AddRange(
            new EmailTemplate
            {
                Type = DomainViewer.Core.Enums.EmailTemplateType.ExpirationAlert,
                Subject = "[แจ้งเตือน] Domain {DomainName} จะหมดอายุในอีก {DaysUntilExpiration} วัน",
                Body = @"<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6;'>
    <h2 style='color: #dc2626;'>⚠️ แจ้งเตือนวันหมดอายุ Domain</h2>
    <p><strong>Domain:</strong> {DomainName}</p>
    <p><strong>วันหมดอายุ:</strong> {ExpirationDate}</p>
    <p><strong>เหลือเวลา:</strong> <span style='color: #dc2626; font-size: 18px;'>{DaysUntilExpiration} วัน</span></p>
    <hr style='margin: 20px 0;'>
    <p style='color: #666; font-size: 12px;'>ข้อความนี้ส่งอัตโนมัติจากระบบ Domain Viewer</p>
</body>
</html>",
                IsEnabled = true,
                UpdatedAt = DateTime.UtcNow
            },
            new EmailTemplate
            {
                Type = DomainViewer.Core.Enums.EmailTemplateType.DomainListReport,
                Subject = "[รายงาน] รายการ Domain ทั้งหมดในระบบ",
                Body = @"<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6;'>
    <h2 style='color: #2563eb;'>📋 รายงานรายการ Domain</h2>
    <p>รายการ Domain ทั้งหมดในระบบ Domain Viewer มีดังนี้:</p>
    {DomainTable}
    <hr style='margin: 20px 0;'>
    <p style='color: #666; font-size: 12px;'>ข้อความนี้ส่งอัตโนมัติจากระบบ Domain Viewer</p>
</body>
</html>",
                IsEnabled = true,
                UpdatedAt = DateTime.UtcNow
            }
        );
    }

    // Seed ExpiredAlert template if not exists
    if (!db.EmailTemplates.Any(t => t.Type == DomainViewer.Core.Enums.EmailTemplateType.ExpiredAlert))
    {
        db.EmailTemplates.Add(new EmailTemplate
        {
            Type = DomainViewer.Core.Enums.EmailTemplateType.ExpiredAlert,
            Subject = "[แจ้งเตือน] Domain {DomainName} หมดอายุแล้ว",
            Body = @"<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6;'>
    <h2 style='color: #dc2626;'>🚨 Domain หมดอายุแล้ว</h2>
    <p><strong>Domain:</strong> {DomainName}</p>
    <p><strong>วันหมดอายุ:</strong> {ExpirationDate}</p>
    <p><strong>สถานะ:</strong> <span style='color: #dc2626; font-size: 18px;'>หมดอายุแล้ว ({DaysUntilExpiration} วัน)</span></p>
    <hr style='margin: 20px 0;'>
    <p style='color: #666; font-size: 12px;'>ข้อความนี้ส่งอัตโนมัติจากระบบ Domain Viewer</p>
</body>
</html>",
            IsEnabled = true,
            UpdatedAt = DateTime.UtcNow
        });
    }

    db.SaveChanges();
}

app.Run();
