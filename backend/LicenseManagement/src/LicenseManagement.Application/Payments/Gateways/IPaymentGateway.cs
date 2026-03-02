namespace LicenseManagement.Application.Payments.Gateways;

public class CreateOrderParams
{
    public string OrderId { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string NotifyUrl { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}

public class CreateOrderResult
{
    public bool Success { get; set; }
    public string? PaymentUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public string? TransactionRef { get; set; }
}

public class CallbackResult
{
    public bool Success { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string TransactionRef { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

public interface IPaymentGateway
{
    string ProviderName { get; }
    Task<CreateOrderResult> CreateOrderAsync(CreateOrderParams request);
    bool VerifyCallback(string payload, string signature);
    CallbackResult ParseCallback(string payload);
}
