using System.Xml.Linq;
using IzweAutoService.Application.Interfaces;
using IzweAutoService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace IzweAutoService.Infrastructure.Services;

public class WirePickSmsGateway : ISmsGateway
{
    private readonly HttpClient _http;
    private readonly ISettingsRepository _settings;
    private readonly ILogger<WirePickSmsGateway> _logger;

    public WirePickSmsGateway(HttpClient http, ISettingsRepository settings, ILogger<WirePickSmsGateway> logger)
    {
        _http = http;
        _settings = settings;
        _logger = logger;
    }

    public async Task<SmsGatewayResult> SendAsync(string phoneNumber, string message, string senderId, string clientId)
    {
        var apiUrl = await _settings.GetValueAsync("ApiUrl") ?? "https://api.wirepick.com/httpsms/send";
        var password = await _settings.GetValueAsync("ApiPassword") ?? "";

        var queryParams = new Dictionary<string, string>
        {
            ["client"] = clientId,
            ["password"] = password,
            ["phone"] = phoneNumber,
            ["text"] = message,
            ["from"] = senderId
        };

        var url = $"{apiUrl}?{string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"))}";

        try
        {
            var response = await _http.GetStringAsync(url);
            return ParseXmlResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS gateway request failed for {Phone}", phoneNumber);
            return new SmsGatewayResult(false, null, null, null, ex.Message);
        }
    }

    private static SmsGatewayResult ParseXmlResponse(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            var sms = doc.Descendants("sms").FirstOrDefault();
            if (sms is null)
                return new SmsGatewayResult(false, null, null, null, "No sms element in response");

            var status = sms.Element("status")?.Value;
            var messageId = sms.Element("messageid")?.Value;
            var cost = decimal.TryParse(sms.Element("cost")?.Value, out var c) ? c : (decimal?)null;

            var success = status?.Equals("0", StringComparison.OrdinalIgnoreCase) == true
                       || status?.Equals("ok", StringComparison.OrdinalIgnoreCase) == true;

            return new SmsGatewayResult(success, messageId, status, cost, success ? null : $"API status: {status}");
        }
        catch (Exception ex)
        {
            return new SmsGatewayResult(false, null, null, null, $"Failed to parse response: {ex.Message}");
        }
    }
}
