using LicenseManagement.Application.Auth.DTOs;
using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Auth.Commands;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ApiResponse<AuthResponse>>
{
    private readonly IAppDbContext _context;
    private readonly IJwtService _jwtService;

    public RefreshTokenCommandHandler(IAppDbContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    public async Task<ApiResponse<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, cancellationToken);

        if (user == null || user.RefreshTokenExpiryTime < DateTime.UtcNow)
            return ApiResponse<AuthResponse>.Fail("Refresh token không hợp lệ hoặc đã hết hạn");

        if (user.IsLocked)
            return ApiResponse<AuthResponse>.Fail("Tài khoản đã bị khóa");

        var accessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<AuthResponse>.Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role.ToString(),
                Balance = user.Balance,
                AvatarUrl = user.AvatarUrl
            }
        });
    }
}
