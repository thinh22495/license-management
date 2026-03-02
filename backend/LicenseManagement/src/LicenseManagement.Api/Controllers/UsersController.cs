using LicenseManagement.Application.Users.Commands;
using LicenseManagement.Application.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenseManagement.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] GetUsersQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserCommand command)
    {
        var result = await _mediator.Send(command);
        return result.Success ? Created("", result) : BadRequest(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpPut("{id}/admin-update")]
    public async Task<IActionResult> AdminUpdateUser(Guid id, [FromBody] AdminUpdateUserCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpPut("{id}/lock")]
    public async Task<IActionResult> LockUser(Guid id, [FromBody] LockUserRequest request)
    {
        var result = await _mediator.Send(new LockUserCommand { UserId = id, IsLocked = request.IsLocked });
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpPost("{id}/topup")]
    public async Task<IActionResult> TopUpBalance(Guid id, [FromBody] AdminTopUpBalanceCommand command)
    {
        command.UserId = id;
        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}

public class LockUserRequest
{
    public bool IsLocked { get; set; }
}
