namespace LicenseManagement.Application.LicensePlans.DTOs;

public class LicensePlanDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public int MaxActivations { get; set; }
    public long Price { get; set; }
    public string Features { get; set; } = "[]";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
