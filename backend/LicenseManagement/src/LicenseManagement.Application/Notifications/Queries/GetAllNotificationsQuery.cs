using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Notifications.Queries;

public class AdminNotificationDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserFullName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GetAllNotificationsQuery : IRequest<ApiResponse<PagedResult<AdminNotificationDto>>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string? Type { get; set; }
}

public class GetAllNotificationsQueryHandler : IRequestHandler<GetAllNotificationsQuery, ApiResponse<PagedResult<AdminNotificationDto>>>
{
    private readonly IAppDbContext _context;

    public GetAllNotificationsQueryHandler(IAppDbContext context) => _context = context;

    public async Task<ApiResponse<PagedResult<AdminNotificationDto>>> Handle(GetAllNotificationsQuery request, CancellationToken ct)
    {
        var query = _context.Notifications
            .Include(n => n.User)
            .OrderByDescending(n => n.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(n =>
                n.Title.ToLower().Contains(search) ||
                n.Body.ToLower().Contains(search) ||
                (n.User != null && n.User.Email.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            query = query.Where(n => n.Type.ToString() == request.Type);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => new AdminNotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                UserEmail = n.User != null ? n.User.Email : null,
                UserFullName = n.User != null ? n.User.FullName : null,
                Title = n.Title,
                Body = n.Body,
                Type = n.Type.ToString(),
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
            })
            .ToListAsync(ct);

        return ApiResponse<PagedResult<AdminNotificationDto>>.Ok(new PagedResult<AdminNotificationDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        });
    }
}
