using LicenseManagement.Application.Auth.DTOs;
using LicenseManagement.Application.Common.Models;
using MediatR;

namespace LicenseManagement.Application.Auth.Commands;

public class LoginCommand : IRequest<ApiResponse<AuthResponse>>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
