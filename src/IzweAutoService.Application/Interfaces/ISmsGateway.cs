namespace IzweAutoService.Application.Interfaces;

public record SmsGatewayResult(bool Success, string? MessageId, string? Status, decimal? Cost, string? Error);

public interface ISmsGateway
{
    Task<SmsGatewayResult> SendAsync(string phoneNumber, string message, string senderId, string clientId);
}
