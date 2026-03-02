using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Licenses.Commands;

public class ValidateLicenseCommand : IRequest<ApiResponse<ValidateResult>>
{
    public string LicenseKey { get; set; } = string.Empty;
    public string HardwareId { get; set; } = string.Empty;
}

public class ValidateResult
{
    public bool Valid { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public string? Tier { get; set; }
    public string[]? Features { get; set; }
}

public class ValidateLicenseCommandHandler : IRequestHandler<ValidateLicenseCommand, ApiResponse<ValidateResult>>
{
    private readonly IAppDbContext _context;

    public ValidateLicenseCommandHandler(IAppDbContext context) => _context = context;

    public async Task<ApiResponse<ValidateResult>> Handle(ValidateLicenseCommand request, CancellationToken ct)
    {
        var license = await _context.UserLicenses
            .Include(ul => ul.User)
            .Include(ul => ul.LicenseProduct)
            .Include(ul => ul.Activations)
            .FirstOrDefaultAsync(ul => ul.LicenseKey == request.LicenseKey, ct);

        if (license == null)
            return ApiResponse<ValidateResult>.Ok(new ValidateResult { Valid = false, Status = "invalid" });

        // Check expiry
        if (license.ExpiresAt.HasValue && license.ExpiresAt.Value < DateTime.UtcNow)
        {
            license.Status = LicenseStatus.Expired;
            await _context.SaveChangesAsync(ct);
            return ApiResponse<ValidateResult>.Ok(new ValidateResult { Valid = false, Status = "expired", ExpiresAt = license.ExpiresAt });
        }

        // Check status
        if (license.Status != LicenseStatus.Active)
            return ApiResponse<ValidateResult>.Ok(new ValidateResult { Valid = false, Status = license.Status.ToString().ToLowerInvariant() });

        // Check user locked
        if (license.User.IsLocked)
            return ApiResponse<ValidateResult>.Ok(new ValidateResult { Valid = false, Status = "user_locked" });

        // Check hardware binding
        var isActivatedOnHardware = license.Activations.Any(a => a.HardwareId == request.HardwareId && a.IsActive);
        if (!isActivatedOnHardware)
            return ApiResponse<ValidateResult>.Ok(new ValidateResult { Valid = false, Status = "not_activated_on_device" });

        var features = System.Text.Json.JsonSerializer.Deserialize<string[]>(license.LicenseProduct.Features);

        return ApiResponse<ValidateResult>.Ok(new ValidateResult
        {
            Valid = true,
            Status = "active",
            ExpiresAt = license.ExpiresAt,
            Tier = license.LicenseProduct.Name,
            Features = features,
        });
    }
}
