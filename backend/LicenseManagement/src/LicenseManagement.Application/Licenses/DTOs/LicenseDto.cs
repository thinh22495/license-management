namespace LicenseManagement.Application.Licenses.DTOs;

public class LicenseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string LicenseKey { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ActivatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int CurrentActivations { get; set; }
    public int MaxActivations { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ActivationResultDto
{
    public string SignedLicenseToken { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
}
