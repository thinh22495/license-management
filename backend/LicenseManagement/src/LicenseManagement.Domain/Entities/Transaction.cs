using LicenseManagement.Domain.Common;
using LicenseManagement.Domain.Enums;

namespace LicenseManagement.Domain.Entities;

public class Transaction : BaseEntity
{
    public Guid UserId { get; set; }
    public TransactionType Type { get; set; }
    public long Amount { get; set; } // VND
    public long BalanceBefore { get; set; }
    public long BalanceAfter { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public string? PaymentRef { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public Guid? RelatedLicenseId { get; set; }
    public string Metadata { get; set; } = "{}"; // JSONB

    // Navigation properties
    public User User { get; set; } = null!;
    public UserLicense? RelatedLicense { get; set; }
}
