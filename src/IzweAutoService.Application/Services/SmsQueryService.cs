using IzweAutoService.Application.DTOs;
using IzweAutoService.Domain.Enums;
using IzweAutoService.Domain.Interfaces;

namespace IzweAutoService.Application.Services;

public class SmsQueryService
{
    private readonly ISmsRepository _repo;

    public SmsQueryService(ISmsRepository repo) => _repo = repo;

    public async Task<SmsPagedResult> GetPagedAsync(int page, int pageSize, SmsStatus? status, string? country, string? search)
    {
        var items = await _repo.GetPagedAsync(page, pageSize, status, country, search);
        var total = await _repo.GetCountAsync(status, country, search);

        return new SmsPagedResult(
            Items: items.Select(r => new SmsRecordDto(
                r.Id, r.ContractId, r.PhoneNumber, r.MessageContent, r.Country,
                r.Status.ToString(), r.ApiMessageId, r.ApiStatus, r.ApiCost,
                r.ErrorMessage, r.RawResponse, r.SourceFile, r.CreatedAt, r.ProcessedAt
            )).ToList(),
            TotalCount: total,
            Page: page,
            PageSize: pageSize
        );
    }
}
