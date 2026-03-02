using LicenseManagement.Application.Auth.DTOs;
using LicenseManagement.Application.Common.Models;
using MediatR;

namespace LicenseManagement.Application.Auth.Commands;

public class RefreshTokenCommand : IRequest<ApiResponse<AuthResponse>>
{
    public string RefreshToken { get; set; } = string.Empty;
}
