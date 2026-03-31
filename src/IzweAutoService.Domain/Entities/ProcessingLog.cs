namespace IzweAutoService.Domain.Entities;

public class ProcessingLog
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalFound { get; set; }
    public int TotalSent { get; set; }
    public int TotalFailed { get; set; }
    public int TotalSkipped { get; set; }
    public string? ErrorMessage { get; set; }
}
