using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Notifications.Queries;

public class GetUnreadCountQuery : IRequest<ApiResponse<int>>
{
    public Guid UserId { get; set; }
}

public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, ApiResponse<int>>
{
    private readonly IAppDbContext _context;

    public GetUnreadCountQueryHandler(IAppDbContext context) => _context = context;

    public async Task<ApiResponse<int>> Handle(GetUnreadCountQuery request, CancellationToken ct)
    {
        var count = await _context.Notifications
            .CountAsync(n => (n.UserId == request.UserId || n.UserId == null) && !n.IsRead, ct);

        return ApiResponse<int>.Ok(count);
    }
}
