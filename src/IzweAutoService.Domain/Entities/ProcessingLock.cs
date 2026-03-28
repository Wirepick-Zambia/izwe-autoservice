namespace IzweAutoService.Domain.Entities;

public class ProcessingLock
{
    public int Id { get; set; }
    public string LockName { get; set; } = string.Empty;
    public string? HeldBy { get; set; }
    public DateTime? AcquiredAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
