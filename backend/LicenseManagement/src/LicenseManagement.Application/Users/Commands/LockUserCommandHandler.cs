using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Users.Commands;

public class LockUserCommandHandler : IRequestHandler<LockUserCommand, ApiResponse>
{
    private readonly IAppDbContext _context;

    public LockUserCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse> Handle(LockUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null)
            return ApiResponse.Fail("User not found");

        user.IsLocked = request.IsLocked;
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok(request.IsLocked ? "User locked" : "User unlocked");
    }
}
