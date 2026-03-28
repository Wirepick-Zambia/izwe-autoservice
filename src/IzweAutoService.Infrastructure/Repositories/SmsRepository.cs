using IzweAutoService.Domain.Entities;
using IzweAutoService.Domain.Enums;
using IzweAutoService.Domain.Interfaces;
using IzweAutoService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IzweAutoService.Infrastructure.Repositories;

public class SmsRepository : ISmsRepository
{
    private readonly AppDbContext _db;

    public SmsRepository(AppDbContext db) => _db = db;

    public async Task<List<SmsRecord>> GetByStatusAsync(SmsStatus status, int batchSize = 100)
        => await _db.SmsRecords
            .Where(r => r.Status == status)
            .OrderBy(r => r.CreatedAt)
            .Take(batchSize)
            .ToListAsync();

    public async Task<SmsRecord?> GetByIdAsync(int id)
        => await _db.SmsRecords.FindAsync(id);

    public async Task<List<SmsRecord>> GetPagedAsync(int page, int pageSize, SmsStatus? status, string? country, string? search)
    {
        var query = BuildQuery(status, country, search);
        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(SmsStatus? status, string? country, string? search)
        => await BuildQuery(status, country, search).CountAsync();

    public async Task AddBatchAsync(IEnumerable<SmsRecord> records, int chunkSize = 5000)
    {
        foreach (var chunk in records.Chunk(chunkSize))
        {
            _db.SmsRecords.AddRange(chunk);
            await _db.SaveChangesAsync();
        }
    }

    public async Task UpdateRangeAsync(IEnumerable<SmsRecord> records)
    {
        _db.SmsRecords.UpdateRange(records);
        await _db.SaveChangesAsync();
    }

    public async Task<Dictionary<SmsStatus, int>> GetStatusCountsAsync()
        => await _db.SmsRecords
            .GroupBy(r => r.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

    public async Task<int> GetTodayCountAsync()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        return await _db.SmsRecords.CountAsync(r => r.CreatedAt >= today && r.CreatedAt < tomorrow);
    }

    public async Task<List<string>> GetDistinctCountriesAsync()
        => await _db.SmsRecords.Select(r => r.Country).Distinct().OrderBy(c => c).ToListAsync();

    private IQueryable<SmsRecord> BuildQuery(SmsStatus? status, string? country, string? search)
    {
        var query = _db.SmsRecords.AsQueryable();
        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        if (!string.IsNullOrEmpty(country)) query = query.Where(r => r.Country == country);
        if (!string.IsNullOrEmpty(search))
            query = query.Where(r => r.PhoneNumber.Contains(search) || r.ContractId.Contains(search));
        return query;
    }
}
