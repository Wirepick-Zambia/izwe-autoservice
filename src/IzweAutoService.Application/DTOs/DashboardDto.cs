namespace IzweAutoService.Application.DTOs;

public record DashboardDto(
    int TotalPending,
    int TotalSent,
    int TotalFailed,
    int TodayCount,
    List<string> Countries,
    List<ProcessingLogDto> RecentLogs
);

public record ProcessingLogDto(
    int Id,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int TotalFound,
    int TotalSent,
    int TotalFailed,
    int TotalSkipped,
    string? ErrorMessage
);
