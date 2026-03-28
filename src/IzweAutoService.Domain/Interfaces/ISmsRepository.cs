using IzweAutoService.Domain.Entities;
using IzweAutoService.Domain.Enums;

namespace IzweAutoService.Domain.Interfaces;

public interface ISmsRepository
{
    Task<List<SmsRecord>> GetByStatusAsync(SmsStatus status, int batchSize = 100);
    Task<SmsRecord?> GetByIdAsync(int id);
    Task<List<SmsRecord>> GetPagedAsync(int page, int pageSize, SmsStatus? status = null, string? country = null, string? search = null);
    Task<int> GetCountAsync(SmsStatus? status = null, string? country = null, string? search = null);
    Task AddBatchAsync(IEnumerable<SmsRecord> records, int chunkSize = 5000);
    Task UpdateRangeAsync(IEnumerable<SmsRecord> records);
    Task<Dictionary<SmsStatus, int>> GetStatusCountsAsync();
    Task<int> GetTodayCountAsync();
    Task<List<string>> GetDistinctCountriesAsync();
}
