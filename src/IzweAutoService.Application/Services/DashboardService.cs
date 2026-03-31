using IzweAutoService.Application.DTOs;
using IzweAutoService.Domain.Interfaces;

namespace IzweAutoService.Application.Services;

public class DashboardService
{
    private readonly ISmsRepository _smsRepo;
    private readonly IProcessingLogRepository _logRepo;

    public DashboardService(ISmsRepository smsRepo, IProcessingLogRepository logRepo)
    {
        _smsRepo = smsRepo;
        _logRepo = logRepo;
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        var counts = await _smsRepo.GetStatusCountsAsync();
        var todayCount = await _smsRepo.GetTodayCountAsync();
        var countries = await _smsRepo.GetDistinctCountriesAsync();
        var logs = await _logRepo.GetRecentAsync(10);

        return new DashboardDto(
            TotalPending: counts.GetValueOrDefault(Domain.Enums.SmsStatus.Pending),
            TotalSent: counts.GetValueOrDefault(Domain.Enums.SmsStatus.Sent),
            TotalFailed: counts.GetValueOrDefault(Domain.Enums.SmsStatus.Failed),
            TodayCount: todayCount,
            Countries: countries,
            RecentLogs: logs.Select(l => new ProcessingLogDto(
                l.Id, l.StartedAt, l.CompletedAt, l.TotalFound, l.TotalSent, l.TotalFailed, l.TotalSkipped, l.ErrorMessage
            )).ToList()
        );
    }
}
