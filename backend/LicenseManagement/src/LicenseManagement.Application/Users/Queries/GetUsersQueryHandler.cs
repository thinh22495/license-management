using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.Users.DTOs;
using LicenseManagement.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Users.Queries;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
{
    private readonly IAppDbContext _context;

    public GetUsersQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrEmpty(request.Search))
        {
            query = query.Where(u =>
                u.Email.Contains(request.Search) ||
                u.FullName.Contains(request.Search) ||
                (u.Phone != null && u.Phone.Contains(request.Search)));
        }

        if (!string.IsNullOrEmpty(request.Role) && Enum.TryParse<UserRole>(request.Role, out var role))
        {
            query = query.Where(u => u.Role == role);
        }

        if (request.IsLocked.HasValue)
        {
            query = query.Where(u => u.IsLocked == request.IsLocked.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                Phone = u.Phone,
                FullName = u.FullName,
                Role = u.Role.ToString(),
                Balance = u.Balance,
                IsLocked = u.IsLocked,
                EmailVerified = u.EmailVerified,
                AvatarUrl = u.AvatarUrl,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<UserDto>
        {
            Items = users,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
