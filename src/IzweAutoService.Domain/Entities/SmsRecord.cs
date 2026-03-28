using IzweAutoService.Domain.Enums;

namespace IzweAutoService.Domain.Entities;

public class SmsRecord
{
    public int Id { get; set; }
    public string ContractId { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string MessageContent { get; set; } = string.Empty;
    public string? MessageTimestamp { get; set; }
    public string Country { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string? SourceFile { get; set; }
    public SmsStatus Status { get; set; } = SmsStatus.Pending;
    public string? ApiMessageId { get; set; }
    public string? ApiStatus { get; set; }
    public decimal? ApiCost { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RawResponse { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}
