using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.Users.DTOs;
using MediatR;

namespace LicenseManagement.Application.Users.Commands;

public class UpdateUserCommand : IRequest<ApiResponse<UserDto>>
{
    public Guid Id { get; set; }
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
}
