using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.Licenses.DTOs;
using LicenseManagement.Domain.Entities;
using LicenseManagement.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Licenses.Commands;

public class ActivateLicenseCommand : IRequest<ApiResponse<ActivationResultDto>>
{
    public string LicenseKey { get; set; } = string.Empty;
    public string HardwareId { get; set; } = string.Empty;
    public string? MachineName { get; set; }
    public string? IpAddress { get; set; }
}

public class ActivateLicenseCommandHandler : IRequestHandler<ActivateLicenseCommand, ApiResponse<ActivationResultDto>>
{
    private readonly IAppDbContext _context;
    private readonly ILicenseCryptoService _cryptoService;

    public ActivateLicenseCommandHandler(IAppDbContext context, ILicenseCryptoService cryptoService)
    {
        _context = context;
        _cryptoService = cryptoService;
    }

    public async Task<ApiResponse<ActivationResultDto>> Handle(ActivateLicenseCommand request, CancellationToken ct)
    {
        var license = await _context.UserLicenses
            .Include(ul => ul.User)
            .Include(ul => ul.LicenseProduct)
                .ThenInclude(lp => lp.Product)
            .Include(ul => ul.Activations)
            .FirstOrDefaultAsync(ul => ul.LicenseKey == request.LicenseKey, ct);

        if (license == null)
            return ApiResponse<ActivationResultDto>.Fail("License key không hợp lệ");

        if (license.Status != LicenseStatus.Active)
            return ApiResponse<ActivationResultDto>.Fail($"License đang ở trạng thái: {license.Status}");

        if (license.User.IsLocked)
            return ApiResponse<ActivationResultDto>.Fail("Tài khoản người dùng đã bị khóa");

        if (license.ExpiresAt.HasValue && license.ExpiresAt.Value < DateTime.UtcNow)
        {
            license.Status = LicenseStatus.Expired;
            await _context.SaveChangesAsync(ct);
            return ApiResponse<ActivationResultDto>.Fail("License đã hết hạn");
        }

        // Check if already activated on this hardware
        var existingActivation = license.Activations
            .FirstOrDefault(a => a.HardwareId == request.HardwareId);

        if (existingActivation != null)
        {
            existingActivation.LastSeenAt = DateTime.UtcNow;
            existingActivation.IsActive = true;
        }
        else
        {
            // Check max activations
            var activeCount = license.Activations.Count(a => a.IsActive);
            if (activeCount >= license.LicenseProduct.MaxActivations)
                return ApiResponse<ActivationResultDto>.Fail(
                    $"Đã đạt giới hạn kích hoạt ({license.LicenseProduct.MaxActivations} thiết bị)");

            var activation = new LicenseActivation
            {
                UserLicenseId = license.Id,
                HardwareId = request.HardwareId,
                MachineName = request.MachineName,
                IpAddress = request.IpAddress,
            };
            _context.LicenseActivations.Add(activation);
            license.CurrentActivations = activeCount + 1;
        }

        license.ActivatedAt ??= DateTime.UtcNow;

        // Get or create signing key for product
        var signingKey = await _context.SigningKeys
            .FirstOrDefaultAsync(sk => sk.ProductId == license.LicenseProduct.ProductId && sk.IsActive, ct);

        if (signingKey == null)
        {
            var (publicKey, privateKeyEnc) = _cryptoService.GenerateKeyPair();
            signingKey = new SigningKey
            {
                ProductId = license.LicenseProduct.ProductId,
                PublicKey = publicKey,
                PrivateKeyEnc = privateKeyEnc,
            };
            _context.SigningKeys.Add(signingKey);
        }

        // Sign the license token
        var payload = new LicensePayload
        {
            Lid = license.Id.ToString(),
            Pid = license.LicenseProduct.ProductId.ToString(),
            Uid = license.UserId.ToString(),
            Tier = license.LicenseProduct.Name,
            Features = System.Text.Json.JsonSerializer.Deserialize<string[]>(license.LicenseProduct.Features) ?? [],
            MaxAct = license.LicenseProduct.MaxActivations,
            Iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Exp = license.ExpiresAt.HasValue
                ? new DateTimeOffset(license.ExpiresAt.Value).ToUnixTimeSeconds()
                : long.MaxValue,
            Hwid = request.HardwareId,
        };

        var signedToken = _cryptoService.SignLicense(payload, signingKey.PrivateKeyEnc);

        // Log event
        _context.LicenseEvents.Add(new LicenseEvent
        {
            UserLicenseId = license.Id,
            EventType = "activated",
            Details = System.Text.Json.JsonSerializer.Serialize(new
            {
                hardwareId = request.HardwareId,
                machineName = request.MachineName
            }),
            IpAddress = request.IpAddress,
        });

        await _context.SaveChangesAsync(ct);

        return ApiResponse<ActivationResultDto>.Ok(new ActivationResultDto
        {
            SignedLicenseToken = signedToken,
            PublicKey = signingKey.PublicKey,
        });
    }
}
