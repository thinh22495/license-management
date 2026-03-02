using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.Users.DTOs;
using LicenseManagement.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Users.Commands;

public class AdminUpdateUserCommand : IRequest<ApiResponse<UserDto>>
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Role { get; set; }
    public bool? EmailVerified { get; set; }
}

public class AdminUpdateUserCommandHandler : IRequestHandler<AdminUpdateUserCommand, ApiResponse<UserDto>>
{
    private readonly IAppDbContext _context;

    public AdminUpdateUserCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<UserDto>> Handle(AdminUpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
        if (user == null)
            return ApiResponse<UserDto>.Fail("Người dùng không tồn tại");

        if (request.Email != null && request.Email != user.Email)
        {
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == request.Email && u.Id != request.Id, cancellationToken);
            if (emailExists)
                return ApiResponse<UserDto>.Fail("Email đã được sử dụng");
            user.Email = request.Email;
        }

        if (request.FullName != null) user.FullName = request.FullName;
        if (request.Phone != null) user.Phone = request.Phone;
        if (request.Role != null) user.Role = request.Role == "Admin" ? UserRole.Admin : UserRole.User;
        if (request.EmailVerified.HasValue) user.EmailVerified = request.EmailVerified.Value;

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
