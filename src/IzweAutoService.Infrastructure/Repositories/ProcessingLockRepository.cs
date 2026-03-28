using IzweAutoService.Domain.Interfaces;
using IzweAutoService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IzweAutoService.Infrastructure.Repositories;

public class ProcessingLockRepository : IProcessingLockRepository
{
    private readonly AppDbContext _db;

    public ProcessingLockRepository(AppDbContext db) => _db = db;

    public async Task<bool> TryAcquireAsync(string lockName, string holderId, TimeSpan expiry)
    {
        var now = DateTime.UtcNow;

        // Atomic update: only acquire if unheld or expired
        var affected = await _db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE processing_locks
            SET HeldBy = {holderId}, AcquiredAt = {now}, ExpiresAt = {now.Add(expiry)}
            WHERE LockName = {lockName}
              AND (HeldBy IS NULL OR ExpiresAt < {now})
            """);

        return affected > 0;
    }

    public async Task ReleaseAsync(string lockName, string holderId)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE processing_locks
            SET HeldBy = NULL, AcquiredAt = NULL, ExpiresAt = NULL
            WHERE LockName = {lockName} AND HeldBy = {holderId}
            """);
    }
}
