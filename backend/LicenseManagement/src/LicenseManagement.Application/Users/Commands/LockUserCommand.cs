using LicenseManagement.Application.Common.Models;
using MediatR;

namespace LicenseManagement.Application.Users.Commands;

public class LockUserCommand : IRequest<ApiResponse>
{
    public Guid UserId { get; set; }
    public bool IsLocked { get; set; }
}
