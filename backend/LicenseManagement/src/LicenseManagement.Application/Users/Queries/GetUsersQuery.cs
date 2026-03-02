using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.Users.DTOs;
using MediatR;

namespace LicenseManagement.Application.Users.Queries;

public class GetUsersQuery : IRequest<PagedResult<UserDto>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string? Role { get; set; }
    public bool? IsLocked { get; set; }
}
