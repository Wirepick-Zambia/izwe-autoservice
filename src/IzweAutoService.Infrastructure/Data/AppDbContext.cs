using IzweAutoService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IzweAutoService.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<SmsRecord> SmsRecords => Set<SmsRecord>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<ProcessingLog> ProcessingLogs => Set<ProcessingLog>();
    public DbSet<ProcessingLock> ProcessingLocks => Set<ProcessingLock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SmsRecord>(e =>
        {
            e.ToTable("sms_records");
            e.Property(r => r.ErrorMessage).HasMaxLength(500);

            // Composite index: most frequent query pattern (pending + oldest first)
            e.HasIndex(r => new { r.Status, r.CreatedAt });
            e.HasIndex(r => r.Country);
            e.HasIndex(r => r.ContractId);
            e.HasIndex(r => r.PhoneNumber);
            e.HasIndex(r => r.ProcessedAt);

            // Prevent duplicate records from reprocessed files
            e.HasIndex(r => new { r.ContractId, r.PhoneNumber, r.SourceFile })
                .HasFilter(null)
                .IsUnique();
        });

        modelBuilder.Entity<AppSetting>(e =>
        {
            e.ToTable("app_settings");
            e.HasIndex(s => s.Key).IsUnique();
            e.HasIndex(s => s.Category);
            e.HasData(
                new AppSetting { Id = 1, Key = "BaseFolderPath", Value = "C:\\SFTP\\SMS_Automation", Category = "General" },
                new AppSetting { Id = 2, Key = "SenderId", Value = "IzweLoans", Category = "Sms" },
                new AppSetting { Id = 3, Key = "ClientId", Value = "WireGhana", Category = "Sms" },
                new AppSetting { Id = 4, Key = "ApiUrl", Value = "https://api.wirepick.com/httpsms/send", Category = "Sms" },
                new AppSetting { Id = 5, Key = "ApiPassword", Value = "", Category = "Sms" },
                new AppSetting { Id = 6, Key = "CronIntervalMinutes", Value = "5", Category = "General" },
                new AppSetting { Id = 7, Key = "SmtpHost", Value = "", Category = "Smtp" },
                new AppSetting { Id = 8, Key = "SmtpPort", Value = "587", Category = "Smtp" },
                new AppSetting { Id = 9, Key = "SmtpUsername", Value = "", Category = "Smtp" },
                new AppSetting { Id = 10, Key = "SmtpPassword", Value = "", Category = "Smtp" },
                new AppSetting { Id = 11, Key = "SmtpUseSsl", Value = "true", Category = "Smtp" },
                new AppSetting { Id = 12, Key = "AlertEmailFrom", Value = "", Category = "Alerts" },
                new AppSetting { Id = 13, Key = "AlertEmailTo", Value = "", Category = "Alerts" },
                new AppSetting { Id = 14, Key = "AlertsEnabled", Value = "false", Category = "Alerts" },
                new AppSetting { Id = 15, Key = "SmsBatchSize", Value = "500", Category = "General" }
            );
        });

        modelBuilder.Entity<ProcessingLog>(e =>
        {
            e.ToTable("processing_logs");
            e.HasIndex(l => l.StartedAt);
        });

        modelBuilder.Entity<ProcessingLock>(e =>
        {
            e.ToTable("processing_locks");
            e.HasIndex(l => l.LockName).IsUnique();
            e.HasData(new ProcessingLock
            {
                Id = 1,
                LockName = "SmsProcessing"
            });
        });
    }
}
