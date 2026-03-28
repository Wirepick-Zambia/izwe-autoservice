namespace IzweAutoService.Application.DTOs;

public record SmsRecordDto(
    int Id,
    string ContractId,
    string PhoneNumber,
    string MessageContent,
    string Country,
    string Status,
    string? ApiMessageId,
    string? ApiStatus,
    decimal? ApiCost,
    string? ErrorMessage,
    string? SourceFile,
    DateTime CreatedAt,
    DateTime? ProcessedAt
);

public record SmsPagedResult(
    List<SmsRecordDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
