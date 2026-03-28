using IzweAutoService.Application.Interfaces;
using IzweAutoService.Application.Services;
using IzweAutoService.Domain.Interfaces;
using IzweAutoService.Infrastructure.Data;
using IzweAutoService.Infrastructure.Repositories;
using IzweAutoService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IzweAutoService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        string provider = "sqlite")
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            switch (provider.ToLowerInvariant())
            {
                case "postgresql":
                case "postgres":
                    options.UseNpgsql(connectionString);
                    break;
                case "mysql":
                    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                    break;
                default:
                    options.UseSqlite(connectionString);
                    break;
            }
        });

        // Repositories
        services.AddScoped<ISmsRepository, SmsRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<IProcessingLogRepository, ProcessingLogRepository>();
        services.AddScoped<IProcessingLockRepository, ProcessingLockRepository>();

        // Infrastructure services
        services.AddHttpClient<ISmsGateway, WirePickSmsGateway>();
        services.AddScoped<IEmailAlertService, SmtpEmailAlertService>();
        services.AddScoped<IFileProcessor, CsvFileProcessor>();

        // Application services
        services.AddScoped<SmsProcessingService>();
        services.AddScoped<SmsQueryService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<SettingsService>();

        return services;
    }
}
