using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.Notifications.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Notifications.Queries;

public class GetMyNotificationsQuery : IRequest<ApiResponse<List<NotificationDto>>>
{
    public Guid UserId { get; set; }
    public bool UnreadOnly { get; set; }
    public int Limit { get; set; } = 50;
}

public class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, ApiResponse<List<NotificationDto>>>
{
    private readonly IAppDbContext _context;

    public GetMyNotificationsQueryHandler(IAppDbContext context) => _context = context;

    public async Task<ApiResponse<List<NotificationDto>>> Handle(GetMyNotificationsQuery request, CancellationToken ct)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == request.UserId || n.UserId == null)
            .OrderByDescending(n => n.CreatedAt)
            .AsQueryable();

        if (request.UnreadOnly)
            query = query.Where(n => !n.IsRead);

        var notifications = await query
            .Take(request.Limit)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Body = n.Body,
                Type = n.Type.ToString(),
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
            })
            .ToListAsync(ct);

        return ApiResponse<List<NotificationDto>>.Ok(notifications);
    }
}
