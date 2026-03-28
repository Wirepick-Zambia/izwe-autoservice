using IzweAutoService.Application.Interfaces;
using IzweAutoService.Domain.Entities;
using IzweAutoService.Domain.Enums;
using IzweAutoService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace IzweAutoService.Application.Services;

public class SmsProcessingService
{
    private readonly ISmsRepository _smsRepo;
    private readonly IProcessingLogRepository _logRepo;
    private readonly ISettingsRepository _settingsRepo;
    private readonly ISmsGateway _smsGateway;
    private readonly IFileProcessor _fileProcessor;
    private readonly IEmailAlertService _emailAlert;
    private readonly ILogger<SmsProcessingService> _logger;

    public SmsProcessingService(
        ISmsRepository smsRepo,
        IProcessingLogRepository logRepo,
        ISettingsRepository settingsRepo,
        ISmsGateway smsGateway,
        IFileProcessor fileProcessor,
        IEmailAlertService emailAlert,
        ILogger<SmsProcessingService> logger)
    {
        _smsRepo = smsRepo;
        _logRepo = logRepo;
        _settingsRepo = settingsRepo;
        _smsGateway = smsGateway;
        _fileProcessor = fileProcessor;
        _emailAlert = emailAlert;
        _logger = logger;
    }

    public async Task ProcessAsync()
    {
        var log = await _logRepo.CreateAsync(new ProcessingLog { StartedAt = DateTime.UtcNow });

        try
        {
            var basePath = await _settingsRepo.GetValueAsync("BaseFolderPath") ?? @"C:\SFTP\SMS_Automation";
            var senderId = await _settingsRepo.GetValueAsync("SenderId") ?? "IzweLoans";
            var clientId = await _settingsRepo.GetValueAsync("ClientId") ?? "WireGhana";
            var batchSizeStr = await _settingsRepo.GetValueAsync("SmsBatchSize") ?? "500";
            var batchSize = int.TryParse(batchSizeStr, out var bs) ? bs : 500;

            // Step 1: Poll for files
            var files = _fileProcessor.PollForFiles(basePath);
            _logger.LogInformation("Found {Count} files to process", files.Count);

            // Step 2: Parse and save records (streamed + chunked inserts)
            var totalParsed = 0;
            foreach (var file in files)
            {
                try
                {
                    var records = _fileProcessor.ParseCsvFile(file, senderId, clientId).ToList();
                    if (records.Count > 0)
                    {
                        await _smsRepo.AddBatchAsync(records);
                        totalParsed += records.Count;
                    }
                    _fileProcessor.MoveToProcessed(file);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse file {File}", file);
                }
            }

            if (totalParsed > 0)
                _logger.LogInformation("Saved {Count} SMS records", totalParsed);

            // Step 3: Send pending messages in batch
            var pending = await _smsRepo.GetByStatusAsync(SmsStatus.Pending, batchSize);
            var sent = 0;
            var failed = 0;

            foreach (var record in pending)
            {
                try
                {
                    var result = await _smsGateway.SendAsync(
                        record.PhoneNumber, record.MessageContent, record.SenderId, record.ClientId);

                    record.ProcessedAt = DateTime.UtcNow;

                    if (result.Success)
                    {
                        record.Status = SmsStatus.Sent;
                        record.ApiMessageId = result.MessageId;
                        record.ApiStatus = result.Status;
                        record.ApiCost = result.Cost;
                        sent++;
                    }
                    else
                    {
                        record.Status = SmsStatus.Failed;
                        record.ErrorMessage = Truncate(result.Error, 500);
                        failed++;
                    }
                }
                catch (Exception ex)
                {
                    record.Status = SmsStatus.Failed;
                    record.ErrorMessage = Truncate(ex.Message, 500);
                    record.ProcessedAt = DateTime.UtcNow;
                    failed++;
                    _logger.LogError(ex, "Failed to send SMS to {Phone}", record.PhoneNumber);
                }
            }

            // Single batch commit for all updates
            if (pending.Count > 0)
                await _smsRepo.UpdateRangeAsync(pending);

            log.TotalFound = totalParsed + pending.Count;
            log.TotalSent = sent;
            log.TotalFailed = failed;
            log.CompletedAt = DateTime.UtcNow;
            await _logRepo.UpdateAsync(log);

            _logger.LogInformation("Processing complete: {Sent} sent, {Failed} failed", sent, failed);

            if (failed > 0)
            {
                await TrySendAlertAsync($"SMS Processing Alert: {failed} failures",
                    $"Processing completed at {DateTime.UtcNow:u}\n\nSent: {sent}\nFailed: {failed}\n\nCheck the portal for details.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS processing failed");
            log.ErrorMessage = Truncate(ex.Message, 500);
            log.CompletedAt = DateTime.UtcNow;
            await _logRepo.UpdateAsync(log);

            await TrySendAlertAsync("SMS Processing Error",
                $"Processing failed at {DateTime.UtcNow:u}\n\nError: {ex.Message}");
        }
    }

    private async Task TrySendAlertAsync(string subject, string body)
    {
        try
        {
            await _emailAlert.SendAlertAsync(subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send email alert — this is non-blocking");
        }
    }

    private static string? Truncate(string? value, int maxLength)
        => value?.Length > maxLength ? value[..maxLength] : value;
}
