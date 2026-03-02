using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LicenseManagement.Application.Payments.Gateways;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LicenseManagement.Infrastructure.Services.Payment;

public class VnPaySettings
{
    public string TmnCode { get; set; } = string.Empty;
    public string HashSecret { get; set; } = string.Empty;
    public string PaymentUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
    public string ApiUrl { get; set; } = "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction";
}

public class VnPayGateway : IPaymentGateway
{
    private readonly VnPaySettings _settings;
    private readonly ILogger<VnPayGateway> _logger;

    public string ProviderName => "VnPay";

    public VnPayGateway(IConfiguration configuration, ILogger<VnPayGateway> logger)
    {
        _settings = configuration.GetSection("Payment:VnPay").Get<VnPaySettings>() ?? new VnPaySettings();
        _logger = logger;
    }

    public Task<CreateOrderResult> CreateOrderAsync(CreateOrderParams request)
    {
        try
        {
            var vnpParams = new SortedDictionary<string, string>
            {
                ["vnp_Version"] = "2.1.0",
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = _settings.TmnCode,
                ["vnp_Amount"] = (request.Amount * 100).ToString(), // VNPay uses amount * 100
                ["vnp_CurrCode"] = "VND",
                ["vnp_TxnRef"] = request.OrderId,
                ["vnp_OrderInfo"] = request.Description,
                ["vnp_OrderType"] = "other",
                ["vnp_Locale"] = "vn",
                ["vnp_ReturnUrl"] = request.ReturnUrl,
                ["vnp_IpAddr"] = request.IpAddress,
                ["vnp_CreateDate"] = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss"),
                ["vnp_ExpireDate"] = DateTime.UtcNow.AddHours(7).AddMinutes(15).ToString("yyyyMMddHHmmss"),
            };

            var queryString = BuildQueryString(vnpParams);
            var signData = queryString;
            var signature = ComputeHmacSha512(signData, _settings.HashSecret);

            var paymentUrl = $"{_settings.PaymentUrl}?{queryString}&vnp_SecureHash={signature}";

            return Task.FromResult(new CreateOrderResult
            {
                Success = true,
                PaymentUrl = paymentUrl,
                TransactionRef = request.OrderId,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VnPay create order failed");
            return Task.FromResult(new CreateOrderResult { Success = false, ErrorMessage = ex.Message });
        }
    }

    public bool VerifyCallback(string payload, string signature)
    {
        try
        {
            var queryParams = ParseQueryString(payload);

            if (!queryParams.TryGetValue("vnp_SecureHash", out var receivedHash))
                return false;

            // Remove hash params for verification
            queryParams.Remove("vnp_SecureHash");
            queryParams.Remove("vnp_SecureHashType");

            var sorted = new SortedDictionary<string, string>(queryParams);
            var signData = BuildQueryString(sorted);
            var computedHash = ComputeHmacSha512(signData, _settings.HashSecret);

            return computedHash.Equals(receivedHash, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VnPay verify callback failed");
            return false;
        }
    }

    public CallbackResult ParseCallback(string payload)
    {
        var queryParams = ParseQueryString(payload);

        queryParams.TryGetValue("vnp_ResponseCode", out var responseCode);
        queryParams.TryGetValue("vnp_TxnRef", out var txnRef);
        queryParams.TryGetValue("vnp_Amount", out var amountStr);
        queryParams.TryGetValue("vnp_TransactionNo", out var transNo);

        var amount = long.TryParse(amountStr, out var a) ? a / 100 : 0; // VNPay amount * 100

        return new CallbackResult
        {
            Success = responseCode == "00",
            OrderId = txnRef ?? "",
            Amount = amount,
            TransactionRef = transNo ?? "",
            ErrorMessage = responseCode != "00" ? $"VnPay response code: {responseCode}" : null,
        };
    }

    private static string BuildQueryString(SortedDictionary<string, string> parameters)
    {
        var sb = new StringBuilder();
        foreach (var (key, value) in parameters)
        {
            if (sb.Length > 0) sb.Append('&');
            sb.Append(WebUtility.UrlEncode(key));
            sb.Append('=');
            sb.Append(WebUtility.UrlEncode(value));
        }
        return sb.ToString();
    }

    private static Dictionary<string, string> ParseQueryString(string query)
    {
        var result = new Dictionary<string, string>();
        var pairs = query.TrimStart('?').Split('&');
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2)
                result[WebUtility.UrlDecode(parts[0])] = WebUtility.UrlDecode(parts[1]);
        }
        return result;
    }

    private static string ComputeHmacSha512(string data, string key)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexStringLower(hash);
    }
}
