using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LicenseManagement.Application.Payments.Gateways;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LicenseManagement.Infrastructure.Services.Payment;

public class ZaloPaySettings
{
    public int AppId { get; set; }
    public string Key1 { get; set; } = string.Empty;
    public string Key2 { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://sb-openapi.zalopay.vn/v2/create";
}

public class ZaloPayGateway : IPaymentGateway
{
    private readonly ZaloPaySettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ZaloPayGateway> _logger;

    public string ProviderName => "ZaloPay";

    public ZaloPayGateway(IConfiguration configuration, HttpClient httpClient, ILogger<ZaloPayGateway> logger)
    {
        _settings = configuration.GetSection("Payment:ZaloPay").Get<ZaloPaySettings>() ?? new ZaloPaySettings();
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CreateOrderResult> CreateOrderAsync(CreateOrderParams request)
    {
        try
        {
            var appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var appTransId = $"{DateTime.UtcNow.AddHours(7):yyMMdd}_{request.OrderId}";
            var embedData = JsonSerializer.Serialize(new { redirecturl = request.ReturnUrl });
            var items = "[]";

            var rawMac = $"{_settings.AppId}|{appTransId}|{request.Description}|{request.Amount}|{appTime}|{embedData}|{items}";
            var mac = ComputeHmacSha256(rawMac, _settings.Key1);

            var formData = new Dictionary<string, string>
            {
                ["app_id"] = _settings.AppId.ToString(),
                ["app_user"] = "license_management",
                ["app_time"] = appTime.ToString(),
                ["amount"] = request.Amount.ToString(),
                ["app_trans_id"] = appTransId,
                ["embed_data"] = embedData,
                ["item"] = items,
                ["description"] = request.Description,
                ["bank_code"] = "",
                ["mac"] = mac,
                ["callback_url"] = request.NotifyUrl,
            };

            var content = new FormUrlEncodedContent(formData);
            var response = await _httpClient.PostAsync(_settings.Endpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("ZaloPay create order response: {Response}", responseBody);

            var result = JsonSerializer.Deserialize<JsonElement>(responseBody);
            var returnCode = result.GetProperty("return_code").GetInt32();

            if (returnCode == 1)
            {
                return new CreateOrderResult
                {
                    Success = true,
                    PaymentUrl = result.GetProperty("order_url").GetString(),
                    TransactionRef = appTransId,
                };
            }

            return new CreateOrderResult
            {
                Success = false,
                ErrorMessage = result.GetProperty("return_message").GetString(),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZaloPay create order failed");
            return new CreateOrderResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public bool VerifyCallback(string payload, string signature)
    {
        try
        {
            var json = JsonSerializer.Deserialize<JsonElement>(payload);
            var data = json.GetProperty("data").GetString() ?? "";
            var computedMac = ComputeHmacSha256(data, _settings.Key2);
            return computedMac == signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZaloPay verify callback failed");
            return false;
        }
    }

    public CallbackResult ParseCallback(string payload)
    {
        var json = JsonSerializer.Deserialize<JsonElement>(payload);
        var dataStr = json.GetProperty("data").GetString() ?? "{}";
        var data = JsonSerializer.Deserialize<JsonElement>(dataStr);

        var appTransId = data.GetProperty("app_trans_id").GetString() ?? "";
        // Extract original orderId from app_trans_id (format: yyMMdd_orderId)
        var orderId = appTransId.Contains('_') ? appTransId[(appTransId.IndexOf('_') + 1)..] : appTransId;

        return new CallbackResult
        {
            Success = true,
            OrderId = orderId,
            Amount = data.GetProperty("amount").GetInt64(),
            TransactionRef = data.GetProperty("zp_trans_id").GetInt64().ToString(),
        };
    }

    private static string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexStringLower(hash);
    }
}
