using IzweAutoService.Application.Services;
using IzweAutoService.Domain.Interfaces;

namespace IzweAutoService.Api.BackgroundServices;

public class SmsProcessingJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SmsProcessingJob> _logger;
    private readonly string _instanceId = $"{Environment.MachineName}-{Environment.ProcessId}";

    public SmsProcessingJob(IServiceScopeFactory scopeFactory, ILogger<SmsProcessingJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SMS Processing Job started (instance: {Id})", _instanceId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var lockRepo = scope.ServiceProvider.GetRequiredService<IProcessingLockRepository>();
                var settings = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();

                // Try to acquire lock (expires after 10 minutes as safety net)
                if (await lockRepo.TryAcquireAsync("SmsProcessing", _instanceId, TimeSpan.FromMinutes(10)))
                {
                    try
                    {
                        var processor = scope.ServiceProvider.GetRequiredService<SmsProcessingService>();
                        await processor.ProcessAsync();
                    }
                    finally
                    {
                        await lockRepo.ReleaseAsync("SmsProcessing", _instanceId);
                    }
                }
                else
                {
                    _logger.LogDebug("Skipping cycle — another instance holds the processing lock");
                }

                var intervalStr = await settings.GetValueAsync("CronIntervalMinutes") ?? "5";
                var interval = int.TryParse(intervalStr, out var mins) ? mins : 5;

                await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMS processing cycle failed — retrying in 1 minute");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("SMS Processing Job stopped");
    }
}
