using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.Users.DTOs;
using LicenseManagement.Domain.Entities;
using LicenseManagement.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Users.Commands;

public class AdminCreateUserCommand : IRequest<ApiResponse<UserDto>>
{
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
}

public class AdminCreateUserCommandHandler : IRequestHandler<AdminCreateUserCommand, ApiResponse<UserDto>>
{
    private readonly IAppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public AdminCreateUserCommandHandler(IAppDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<ApiResponse<UserDto>> Handle(AdminCreateUserCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (existingUser != null)
            return ApiResponse<UserDto>.Fail("Email đã được sử dụng");

        if (!string.IsNullOrEmpty(request.Phone))
        {
            var phoneExists = await _context.Users
                .AnyAsync(u => u.Phone == request.Phone, cancellationToken);
            if (phoneExists)
                return ApiResponse<UserDto>.Fail("Số điện thoại đã được sử dụng");
        }

        var role = request.Role == "Admin" ? UserRole.Admin : UserRole.User;

        var user = new User
        {
            Email = request.Email,
            Phone = string.IsNullOrEmpty(request.Phone) ? null : request.Phone,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FullName = request.FullName,
            Role = role,
            EmailVerified = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<UserDto>.Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Phone = user.Phone,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            Balance = user.Balance,
            IsLocked = user.IsLocked,
            EmailVerified = user.EmailVerified,
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        });
    }
}
