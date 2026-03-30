using IzweAutoService.Application.Interfaces;
using IzweAutoService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace IzweAutoService.Infrastructure.Services;

public class CsvFileProcessor : IFileProcessor
{
    private readonly ILogger<CsvFileProcessor> _logger;

    public CsvFileProcessor(ILogger<CsvFileProcessor> logger) => _logger = logger;

    public List<string> PollForFiles(string baseFolderPath)
    {
        var files = new List<string>();

        if (!Directory.Exists(baseFolderPath))
        {
            _logger.LogWarning("Base folder does not exist: {Path}", baseFolderPath);
            return files;
        }

        foreach (var countryDir in Directory.GetDirectories(baseFolderPath))
        {
            var dirName = Path.GetFileName(countryDir);
            if (dirName is "pending" or "processed") continue;

            var pendingDir = Path.Combine(countryDir, "pending");
            Directory.CreateDirectory(pendingDir);

            // Move new files from country root into pending/
            foreach (var file in Directory.GetFiles(countryDir, "*.csv"))
            {
                var dest = Path.Combine(pendingDir, Path.GetFileName(file));
                File.Move(file, dest, overwrite: true);
                _logger.LogInformation("Moved {File} to pending", Path.GetFileName(file));
            }

            // Pick up all files in pending/ (including those placed there directly)
            foreach (var file in Directory.GetFiles(pendingDir, "*.csv"))
            {
                files.Add(file);
            }
        }

        return files;
    }

    public IEnumerable<SmsRecord> ParseCsvFile(string filePath, string senderId, string clientId)
    {
        var country = ExtractCountry(filePath);
        var fileName = Path.GetFileName(filePath);
        var count = 0;

        foreach (var line in File.ReadLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var delimiter = line.Contains('|') ? '|' : ',';
            var parts = line.Split(delimiter);
            if (parts.Length < 3)
            {
                _logger.LogWarning("Skipping malformed line in {File}: {Line}", fileName, line);
                continue;
            }

            count++;
            yield return new SmsRecord
            {
                ContractId = parts[0].Trim(),
                PhoneNumber = parts[1].Trim(),
                MessageContent = parts[2].Trim(),
                MessageTimestamp = parts.Length > 3 ? parts[3].Trim() : null,
                Country = country,
                SenderId = senderId,
                ClientId = clientId,
                SourceFile = fileName
            };
        }

        _logger.LogInformation("Parsed {Count} records from {File}", count, fileName);
    }

    public void MoveToProcessed(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath)!;
        var parentDir = Path.GetDirectoryName(dir)!;
        var processedDir = Path.Combine(parentDir, "processed");
        Directory.CreateDirectory(processedDir);

        var dest = Path.Combine(processedDir, $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Path.GetFileName(filePath)}");
        File.Move(filePath, dest);
    }

    private static string ExtractCountry(string filePath)
    {
        var parts = filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Length >= 3 ? parts[^3] : "Unknown";
    }
}
