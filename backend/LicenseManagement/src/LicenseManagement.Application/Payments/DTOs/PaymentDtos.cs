namespace LicenseManagement.Application.Payments.DTOs;

public class TopUpRequestDto
{
    public long Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty; // momo, vnpay, zalopay
    public string ReturnUrl { get; set; } = string.Empty;
}

public class TopUpResultDto
{
    public Guid TransactionId { get; set; }
    public string? PaymentUrl { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class TransactionDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public long Amount { get; set; }
    public long BalanceBefore { get; set; }
    public long BalanceAfter { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentRef { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? RelatedLicenseId { get; set; }
    public DateTime CreatedAt { get; set; }
}
