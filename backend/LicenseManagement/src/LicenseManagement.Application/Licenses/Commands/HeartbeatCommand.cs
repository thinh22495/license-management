using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Licenses.Commands;

public class HeartbeatCommand : IRequest<ApiResponse<HeartbeatResponse>>
{
    public string LicenseKey { get; set; } = string.Empty;
    public string HardwareId { get; set; } = string.Empty;
}

public class HeartbeatResponse
{
    public string Status { get; set; } = string.Empty;
    public string? SignedLicenseToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool NeedsReactivation { get; set; }
}

public class HeartbeatCommandHandler : IRequestHandler<HeartbeatCommand, ApiResponse<HeartbeatResponse>>
{
    private readonly IAppDbContext _context;
    private readonly ILicenseCryptoService _cryptoService;

    public HeartbeatCommandHandler(IAppDbContext context, ILicenseCryptoService cryptoService)
    {
        _context = context;
        _cryptoService = cryptoService;
    }

    public async Task<ApiResponse<HeartbeatResponse>> Handle(HeartbeatCommand request, CancellationToken ct)
    {
        var license = await _context.UserLicenses
            .Include(ul => ul.User)
            .Include(ul => ul.LicenseProduct)
            .Include(ul => ul.Activations)
            .FirstOrDefaultAsync(ul => ul.LicenseKey == request.LicenseKey, ct);

        if (license == null)
            return ApiResponse<HeartbeatResponse>.Fail("License key không hợp lệ");

        // Check if expired
        if (license.ExpiresAt.HasValue && license.ExpiresAt.Value < DateTime.UtcNow)
        {
            license.Status = LicenseStatus.Expired;
            await _context.SaveChangesAsync(ct);
            return ApiResponse<HeartbeatResponse>.Ok(new HeartbeatResponse
            {
                Status = "expired",
                ExpiresAt = license.ExpiresAt,
            });
        }

        // Check if revoked/suspended
        if (license.Status is LicenseStatus.Revoked or LicenseStatus.Suspended)
        {
            return ApiResponse<HeartbeatResponse>.Ok(new HeartbeatResponse
            {
                Status = license.Status.ToString().ToLowerInvariant(),
                NeedsReactivation = license.Status == LicenseStatus.Suspended,
            });
        }

        // Update last seen
        var activation = license.Activations
            .FirstOrDefault(a => a.HardwareId == request.HardwareId && a.IsActive);

        if (activation != null)
            activation.LastSeenAt = DateTime.UtcNow;

        // Refresh token if expiring within 7 days
        string? refreshedToken = null;
        if (license.ExpiresAt.HasValue &&
            license.ExpiresAt.Value < DateTime.UtcNow.AddDays(7) &&
            license.ExpiresAt.Value > DateTime.UtcNow)
        {
            var signingKey = await _context.SigningKeys
                .FirstOrDefaultAsync(sk => sk.ProductId == license.LicenseProduct.ProductId && sk.IsActive, ct);

            if (signingKey != null)
            {
                var payload = new LicensePayload
                {
                    Lid = license.Id.ToString(),
                    Pid = license.LicenseProduct.ProductId.ToString(),
                    Uid = license.UserId.ToString(),
                    Tier = license.LicenseProduct.Name,
                    MaxAct = license.LicenseProduct.MaxActivations,
                    Iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Exp = new DateTimeOffset(license.ExpiresAt.Value).ToUnixTimeSeconds(),
                    Hwid = request.HardwareId,
                };
                refreshedToken = _cryptoService.SignLicense(payload, signingKey.PrivateKeyEnc);
            }
        }

        await _context.SaveChangesAsync(ct);

        return ApiResponse<HeartbeatResponse>.Ok(new HeartbeatResponse
        {
            Status = "active",
            ExpiresAt = license.ExpiresAt,
            SignedLicenseToken = refreshedToken,
        });
    }
}
