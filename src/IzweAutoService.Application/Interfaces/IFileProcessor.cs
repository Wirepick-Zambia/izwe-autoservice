using IzweAutoService.Domain.Entities;

namespace IzweAutoService.Application.Interfaces;

public interface IFileProcessor
{
    List<string> PollForFiles(string baseFolderPath);
    IEnumerable<SmsRecord> ParseCsvFile(string filePath, string senderId, string clientId);
    void MoveToProcessed(string filePath);
}
