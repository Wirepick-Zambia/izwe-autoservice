using System.Globalization;
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
            var trimmed = raw.TrimStart();

            // XML format:
            // <messages><sms><msgid>...</msgid><phone>...</phone><status>ACT</status>
            //   <recd_time>...</recd_time><unit_price>0.01</unit_price>
            //   <num_sms>1</num_sms><total_cost>0.01</total_cost><currency>USD</currency></sms></messages>
            if (trimmed.StartsWith('<'))
                return ParseXmlResponse(raw, phoneNumber);

            // Plain text fallback (space-delimited):
            // MessageId Phone Status Date Time Cost Qty UnitPrice Currency
            return ParseTextResponse(raw, phoneNumber);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse gateway response: {Response}", raw);
            return new SmsGatewayResult(false, null, null, null, $"Parse error: {ex.Message}", raw);
        }
    }

    private SmsGatewayResult ParseXmlResponse(string raw, string phoneNumber)
    {
        var doc = XDocument.Parse(raw);
        var smsElements = doc.Descendants("sms").ToList();

        if (smsElements.Count == 0)
            return new SmsGatewayResult(false, null, null, null, "No <sms> element in response", raw);

        // Match by phone number if multiple results, otherwise take the first
        var sms = smsElements.Count > 1
            ? smsElements.FirstOrDefault(e =>
            {
                var p = e.Element("phone")?.Value ?? "";
                return p == phoneNumber || phoneNumber.EndsWith(p) || p.EndsWith(phoneNumber.TrimStart('+'));
            }) ?? smsElements[0]
            : smsElements[0];

        var messageId = sms.Element("msgid")?.Value;
        var statusCode = sms.Element("status")?.Value ?? "";
        var totalCost = decimal.TryParse(sms.Element("total_cost")?.Value, CultureInfo.InvariantCulture, out var c) ? c : (decimal?)null;
        var success = SuccessStatuses.Contains(statusCode);

        var statusDisplay = StatusDescriptions.TryGetValue(statusCode, out var desc)
            ? $"{statusCode} — {desc}"
            : statusCode;

        return new SmsGatewayResult(
            success,
            messageId,
            statusDisplay,
            totalCost,
            success ? null : statusDisplay,
            raw
        );
    }

    private static SmsGatewayResult ParseTextResponse(string raw, string phoneNumber)
    {
        // Format: MessageId Phone Status Date Time Cost Qty UnitPrice Currency
        var lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 5) continue;

            var responsePhone = parts[1];
            if (responsePhone == phoneNumber || phoneNumber.EndsWith(responsePhone) || responsePhone.EndsWith(phoneNumber.TrimStart('+')))
                return BuildTextResult(parts, raw);
        }

        // Single SMS — take the first valid line
        foreach (var line in lines)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 5)
                return BuildTextResult(parts, raw);
        }

        return new SmsGatewayResult(false, null, null, null, "Could not parse response", raw);
    }

    private static SmsGatewayResult BuildTextResult(string[] parts, string raw)
    {
        var messageId = parts[0];
        var statusCode = parts[2];
        var cost = parts.Length > 5 ? decimal.TryParse(parts[5], CultureInfo.InvariantCulture, out var c) ? c : (decimal?)null : null;
        var success = SuccessStatuses.Contains(statusCode);

        var statusDisplay = StatusDescriptions.TryGetValue(statusCode, out var desc)
            ? $"{statusCode} — {desc}"
            : statusCode;

        return new SmsGatewayResult(success, messageId, statusDisplay, cost, success ? null : statusDisplay, raw);
    }
}
