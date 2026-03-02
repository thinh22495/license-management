using System.Security.Claims;
using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.Users.Commands;
using LicenseManagement.Application.Users.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAppDbContext _context;

    public MeController(IMediator mediator, IAppDbContext context)
    {
        _mediator = mediator;
        _context = context;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound();

        return Ok(ApiResponse<UserDto>.Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Phone = user.Phone,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            Balance = user.Balance,
            IsLocked = user.IsLocked,
            EmailVerified = user.EmailVerified,
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        }));
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserCommand command)
    {
        command.Id = GetUserId();
        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        var userId = GetUserId();
        var balance = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.Balance)
            .FirstOrDefaultAsync();

        return Ok(ApiResponse<object>.Ok(new { Balance = balance }));
    }
}
