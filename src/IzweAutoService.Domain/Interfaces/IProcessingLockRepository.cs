namespace IzweAutoService.Domain.Interfaces;

public interface IProcessingLockRepository
{
    Task<bool> TryAcquireAsync(string lockName, string holderId, TimeSpan expiry);
    Task ReleaseAsync(string lockName, string holderId);
}
