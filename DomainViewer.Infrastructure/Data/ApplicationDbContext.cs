using DomainViewer.Core.Entities;
using DomainViewer.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace DomainViewer.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Domain> Domains => Set<Domain>();
    public DbSet<GlobalAlertSetting> GlobalAlertSettings => Set<GlobalAlertSetting>();
    public DbSet<DomainNotificationRecipient> DomainNotificationRecipients => Set<DomainNotificationRecipient>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<DomainAttachment> DomainAttachments => Set<DomainAttachment>();
    public DbSet<AllowedEmailDomain> AllowedEmailDomains => Set<AllowedEmailDomain>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Users
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.ExternalId).IsUnique();
            entity.HasIndex(e => e.Role);
            entity.Property(e => e.Role).HasConversion<string>();
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Provider).HasMaxLength(50);
            entity.Property(e => e.ExternalId).HasMaxLength(255);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
        });

        // Domains
        modelBuilder.Entity<Domain>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.ExpirationDate);
            entity.HasIndex(e => e.IsActive);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Registrant).HasMaxLength(255);
            entity.Property(e => e.Registrar).HasMaxLength(200);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);

            entity.HasOne(e => e.Creator)
                  .WithMany(u => u.CreatedDomains)
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // GlobalAlertSettings
        modelBuilder.Entity<GlobalAlertSetting>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        // DomainNotificationRecipients
        modelBuilder.Entity<DomainNotificationRecipient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DomainId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.DomainId, e.UserId }).IsUnique();

            entity.HasOne(e => e.Domain)
                  .WithMany(d => d.DomainNotificationRecipients)
                  .HasForeignKey(e => e.DomainId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.DomainNotificationRecipients)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // NotificationLogs
        modelBuilder.Entity<NotificationLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DomainId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.SentAt);
            entity.HasIndex(e => new { e.DomainId, e.UserId, e.AlertType, e.ExpirationDate }).IsUnique();
            entity.Property(e => e.AlertType).HasMaxLength(20);
            entity.Property(e => e.Status).HasConversion<string>();

            entity.HasOne(e => e.Domain)
                  .WithMany(d => d.NotificationLogs)
                  .HasForeignKey(e => e.DomainId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.NotificationLogs)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // DomainAttachments
        modelBuilder.Entity<DomainAttachment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DomainId);
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.ContentType).HasMaxLength(100);

            entity.HasOne(e => e.Domain)
                  .WithMany(d => d.DomainAttachments)
                  .HasForeignKey(e => e.DomainId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // RefreshTokens
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ExpiresAt);
            entity.Property(e => e.Token).HasMaxLength(255);
            entity.Property(e => e.ReplacedByToken).HasMaxLength(255);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // AllowedEmailDomains
        modelBuilder.Entity<AllowedEmailDomain>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Domain).IsUnique();
            entity.Property(e => e.Domain).HasMaxLength(100);
        });

        // EmailTemplates
        modelBuilder.Entity<EmailTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Type).IsUnique();
            entity.Property(e => e.Type).HasConversion<string>();
            entity.Property(e => e.Subject).HasMaxLength(500);
        });
    }
}
