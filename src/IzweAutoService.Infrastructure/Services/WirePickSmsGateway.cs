using System.Globalization;
using IzweAutoService.Application.Interfaces;
using IzweAutoService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace IzweAutoService.Infrastructure.Services;

public class WirePickSmsGateway : ISmsGateway
{
    private readonly HttpClient _http;
    private readonly ISettingsRepository _settings;
    private readonly ILogger<WirePickSmsGateway> _logger;

    // Status codes that mean the SMS was accepted/delivered
    private static readonly HashSet<string> SuccessStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "ACT", "DLV", "DLA", "DLG", "BUF", "IPR", "ITM", "PACT"
    };

    // Human-readable descriptions for all known status codes
    private static readonly Dictionary<string, string> StatusDescriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ABS"] = "Absent Subscriber",
        ["ACT"] = "Accepted into Wirepick Server",
        ["BLO"] = "Destination number blocked by Telecom or Wirepick",
        ["BUF"] = "Buffered by gateway for delayed delivery",
        ["DLA"] = "Delivered by Alert App",
        ["DLG"] = "Delivered to upstream gateway",
        ["DLV"] = "Delivered to recipient handset",
        ["DUP"] = "Duplicate message received from client",
        ["ERD"] = "Error delivering message to handset",
        ["EXP"] = "Message expired, could not be delivered",
        ["INF"] = "Invalid Affiliate — not configured in client profile",
        ["INV"] = "Invalid Network Address — unknown or unreachable",
        ["IPR"] = "In process at Wirepick",
        ["IPV"] = "Invalid Remote IP — not registered",
        ["ITM"] = "Intermediate processing",
        ["LEN"] = "Message too long",
        ["MAP"] = "Daily maximum reached at provider",
        ["MAX"] = "Daily maximum reached",
        ["NCR"] = "Not configured in client routes",
        ["NPZ"] = "Network price not configured",
        ["NRC"] = "No receipt confirmation from Telecom",
        ["NSF"] = "Out of credit — please repurchase credits",
        ["PACT"] = "Notification accepted into the system",
        ["PAPP"] = "Delivered to PickAlert mobile notification",
        ["PDLV"] = "Delivered to subscriber handset",
        ["PDUP"] = "Duplicate notification in PickAlert system",
        ["PERR"] = "Error sending notification to smartphone",
        ["PHN"] = "Invalid phone number length",
        ["PIPV"] = "Invalid Remote IP in PickAlert platform",
        ["PNP"] = "Provider not provisioned — temporarily not accepting traffic",
        ["PNSC"] = "No PickAlert Subscriber with destination phone",
        ["PNSF"] = "Insufficient fund in PickAlert platform",
        ["PQUD"] = "Queued to be delivered to the App",
        ["PRED"] = "Read by PickAlert user on smartphone",
        ["PTER"] = "Error sending token via Wirepick gateway",
        ["PTOK"] = "Token mismatched",
        ["PWD"] = "No or invalid password",
        ["PXP"] = "Password expired",
        ["REJ"] = "Rejected by Telecom",
        ["RTE"] = "Routing error at gateway or network",
        ["RTN"] = "Route not configured",
        ["SME"] = "Submission error — Telecom rejected the message",
        ["SND"] = "Sender ID is not registered",
        ["STP"] = "Stopped — requested by client",
        ["UNK"] = "Unknown Network",
        ["USB"] = "Unknown Subscriber — number may no longer be in use",
    };

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
            _logger.LogDebug("Gateway response for {Phone}: {Response}", phoneNumber, response);
            return ParseResponse(response, phoneNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS gateway request failed for {Phone}", phoneNumber);
            return new SmsGatewayResult(false, null, null, null, ex.Message, null);
        }
    }

    private SmsGatewayResult ParseResponse(string raw, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new SmsGatewayResult(false, null, null, null, "Empty response from gateway", raw);

        try
        {
            // Response format (space-delimited, one result per line or concatenated):
            // MessageId Phone Status Date Time Cost Qty UnitPrice Currency
            // e.g.: 6442080299942398490 260979263249 ACT 2026-03-28 11:58:57 0.03 1 0.03 USD

            var lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // Find the result line matching this phone number
            foreach (var line in lines)
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5) continue;

                var responsePhone = parts[1];
                if (responsePhone == phoneNumber || phoneNumber.EndsWith(responsePhone) || responsePhone.EndsWith(phoneNumber.TrimStart('+')))
                    return ParseResultParts(parts, raw);
            }

            // Single SMS send typically returns one line — parse the first valid result
            foreach (var line in lines)
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 5)
                    return ParseResultParts(parts, raw);
            }

            return new SmsGatewayResult(false, null, null, null, $"Could not parse response", raw);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse gateway response: {Response}", raw);
            return new SmsGatewayResult(false, null, null, null, $"Parse error: {ex.Message}", raw);
        }
    }

    private static SmsGatewayResult ParseResultParts(string[] parts, string raw)
    {
        // parts: [0]=MessageId [1]=Phone [2]=Status [3]=Date [4]=Time [5]=Cost [6]=Qty [7]=UnitPrice [8]=Currency
        var messageId = parts[0];
        var statusCode = parts[2];
        var cost = parts.Length > 5 ? decimal.TryParse(parts[5], CultureInfo.InvariantCulture, out var c) ? c : (decimal?)null : null;
        var success = SuccessStatuses.Contains(statusCode);

        var statusDisplay = StatusDescriptions.TryGetValue(statusCode, out var desc)
            ? $"{statusCode} — {desc}"
            : statusCode;

        return new SmsGatewayResult(
            success,
            messageId,
            statusDisplay,
            cost,
            success ? null : statusDisplay,
            raw
        );
    }
}
