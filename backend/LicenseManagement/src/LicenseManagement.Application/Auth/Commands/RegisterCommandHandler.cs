using LicenseManagement.Application.Auth.DTOs;
using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Domain.Entities;
using LicenseManagement.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Auth.Commands;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, ApiResponse<AuthResponse>>
{
    private readonly IAppDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterCommandHandler(IAppDbContext context, IJwtService jwtService, IPasswordHasher passwordHasher)
    {
        _context = context;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
    }

    public async Task<ApiResponse<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check if email already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (existingUser != null)
            return ApiResponse<AuthResponse>.Fail("Email đã được sử dụng");

        // Check if phone already exists
        if (!string.IsNullOrEmpty(request.Phone))
        {
            var phoneExists = await _context.Users
                .AnyAsync(u => u.Phone == request.Phone, cancellationToken);
            if (phoneExists)
                return ApiResponse<AuthResponse>.Fail("Số điện thoại đã được sử dụng");
        }

        var user = new User
        {
            Email = request.Email,
            Phone = string.IsNullOrEmpty(request.Phone) ? null : request.Phone,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FullName = request.FullName,
            Role = UserRole.User
        };

        var refreshToken = _jwtService.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtService.GenerateAccessToken(user);

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
