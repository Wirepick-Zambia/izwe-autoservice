using IzweAutoService.Domain.Entities;
using IzweAutoService.Domain.Interfaces;
using IzweAutoService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IzweAutoService.Infrastructure.Repositories;

public class ProcessingLogRepository : IProcessingLogRepository
{
    private readonly AppDbContext _db;

    public ProcessingLogRepository(AppDbContext db) => _db = db;

    public async Task<ProcessingLog> CreateAsync(ProcessingLog log)
    {
        _db.ProcessingLogs.Add(log);
        await _db.SaveChangesAsync();
        return log;
    }

    public async Task UpdateAsync(ProcessingLog log)
    {
        // Detach any other tracked entities that may be in a failed state,
        // so this save always succeeds (critical for error recording)
        foreach (var entry in _db.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is not ProcessingLog)
                entry.State = EntityState.Detached;
        }

        _db.ProcessingLogs.Update(log);
        await _db.SaveChangesAsync();
    }

    public async Task<List<ProcessingLog>> GetRecentAsync(int count = 20)
        => await _db.ProcessingLogs
            .OrderByDescending(l => l.StartedAt)
            .Take(count)
            .ToListAsync();
}
