using LicenseManagement.Application.Auth.DTOs;
using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Auth.Commands;

public class LoginCommandHandler : IRequestHandler<LoginCommand, ApiResponse<AuthResponse>>
{
    private readonly IAppDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;

    public LoginCommandHandler(IAppDbContext context, IJwtService jwtService, IPasswordHasher passwordHasher)
    {
        _context = context;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
    }

    public async Task<ApiResponse<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            return ApiResponse<AuthResponse>.Fail("Email hoặc mật khẩu không đúng");

        if (user.IsLocked)
            return ApiResponse<AuthResponse>.Fail("Tài khoản đã bị khóa");

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<AuthResponse>.Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
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
