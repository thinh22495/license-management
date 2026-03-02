using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LicenseManagement.Application.Payments.Gateways;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LicenseManagement.Infrastructure.Services.Payment;

public class MoMoSettings
{
    public string PartnerCode { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://test-payment.momo.vn/v2/gateway/api/create";
}

public class MoMoGateway : IPaymentGateway
{
    private readonly MoMoSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<MoMoGateway> _logger;

    public string ProviderName => "MoMo";

    public MoMoGateway(IConfiguration configuration, HttpClient httpClient, ILogger<MoMoGateway> logger)
    {
        _settings = configuration.GetSection("Payment:MoMo").Get<MoMoSettings>() ?? new MoMoSettings();
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CreateOrderResult> CreateOrderAsync(CreateOrderParams request)
    {
        try
        {
            var requestId = Guid.NewGuid().ToString();
            var orderId = request.OrderId;
            var orderInfo = request.Description;
            var amount = request.Amount;
            var extraData = "";
            var requestType = "payWithMethod";

            // Build raw signature
            var rawSignature = $"accessKey={_settings.AccessKey}&amount={amount}&extraData={extraData}" +
                               $"&ipnUrl={request.NotifyUrl}&orderId={orderId}&orderInfo={orderInfo}" +
                               $"&partnerCode={_settings.PartnerCode}&redirectUrl={request.ReturnUrl}" +
                               $"&requestId={requestId}&requestType={requestType}";

            var signature = ComputeHmacSha256(rawSignature, _settings.SecretKey);

            var body = new
            {
                partnerCode = _settings.PartnerCode,
                partnerName = "License Management",
                storeId = _settings.PartnerCode,
                requestId,
                amount,
                orderId,
                orderInfo,
                redirectUrl = request.ReturnUrl,
                ipnUrl = request.NotifyUrl,
                lang = "vi",
                requestType,
                autoCapture = true,
                extraData,
                signature,
            };

            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_settings.Endpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("MoMo create order response: {Response}", responseBody);

            var result = JsonSerializer.Deserialize<JsonElement>(responseBody);
            var resultCode = result.GetProperty("resultCode").GetInt32();

            if (resultCode == 0)
            {
                return new CreateOrderResult
                {
                    Success = true,
                    PaymentUrl = result.GetProperty("payUrl").GetString(),
                    TransactionRef = requestId,
                };
            }

            return new CreateOrderResult
            {
                Success = false,
                ErrorMessage = result.GetProperty("message").GetString(),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MoMo create order failed");
            return new CreateOrderResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public bool VerifyCallback(string payload, string signature)
    {
        try
        {
            var json = JsonSerializer.Deserialize<JsonElement>(payload);
            var accessKey = _settings.AccessKey;
            var amount = json.GetProperty("amount").GetInt64();
            var extraData = json.GetProperty("extraData").GetString() ?? "";
            var message = json.GetProperty("message").GetString() ?? "";
            var orderId = json.GetProperty("orderId").GetString() ?? "";
            var orderInfo = json.GetProperty("orderInfo").GetString() ?? "";
            var orderType = json.GetProperty("orderType").GetString() ?? "";
            var partnerCode = json.GetProperty("partnerCode").GetString() ?? "";
            var payType = json.GetProperty("payType").GetString() ?? "";
            var requestId = json.GetProperty("requestId").GetString() ?? "";
            var responseTime = json.GetProperty("responseTime").GetInt64();
            var resultCode = json.GetProperty("resultCode").GetInt32();
            var transId = json.GetProperty("transId").GetInt64();

            var rawSignature = $"accessKey={accessKey}&amount={amount}&extraData={extraData}" +
                               $"&message={message}&orderId={orderId}&orderInfo={orderInfo}" +
                               $"&orderType={orderType}&partnerCode={partnerCode}&payType={payType}" +
                               $"&requestId={requestId}&responseTime={responseTime}&resultCode={resultCode}" +
                               $"&transId={transId}";

            var computedSignature = ComputeHmacSha256(rawSignature, _settings.SecretKey);
            return computedSignature == signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MoMo verify callback failed");
            return false;
        }
    }

    public CallbackResult ParseCallback(string payload)
    {
        var json = JsonSerializer.Deserialize<JsonElement>(payload);
        var resultCode = json.GetProperty("resultCode").GetInt32();

        return new CallbackResult
        {
            Success = resultCode == 0,
            OrderId = json.GetProperty("orderId").GetString() ?? "",
            Amount = json.GetProperty("amount").GetInt64(),
            TransactionRef = json.GetProperty("transId").GetInt64().ToString(),
            ErrorMessage = resultCode != 0 ? json.GetProperty("message").GetString() : null,
        };
    }

    private static string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexStringLower(hash);
    }
}
