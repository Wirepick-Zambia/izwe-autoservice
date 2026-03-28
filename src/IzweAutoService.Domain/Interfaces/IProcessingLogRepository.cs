using IzweAutoService.Domain.Entities;

namespace IzweAutoService.Domain.Interfaces;

public interface IProcessingLogRepository
{
    Task<ProcessingLog> CreateAsync(ProcessingLog log);
    Task UpdateAsync(ProcessingLog log);
    Task<List<ProcessingLog>> GetRecentAsync(int count = 20);
}
