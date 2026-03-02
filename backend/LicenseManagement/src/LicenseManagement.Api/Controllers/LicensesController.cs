using System.Security.Claims;
using LicenseManagement.Application.Licenses.Commands;
using LicenseManagement.Application.Licenses.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenseManagement.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class LicensesController : ControllerBase
{
    private readonly IMediator _mediator;

    public LicensesController(IMediator mediator) => _mediator = mediator;

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>Purchase a license (deducts balance)</summary>
    [HttpPost("purchase")]
    [Authorize]
    public async Task<IActionResult> Purchase([FromBody] PurchaseLicenseCommand command)
    {
        command.UserId = GetUserId();
        var result = await _mediator.Send(command);
        return result.Success ? Created("", result) : BadRequest(result);
    }

    /// <summary>Activate license on a device (client calls this)</summary>
    [HttpPost("activate")]
    public async Task<IActionResult> Activate([FromBody] ActivateLicenseCommand command)
    {
        command.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Deactivate license on a device</summary>
    [HttpPost("deactivate")]
    public async Task<IActionResult> Deactivate([FromBody] DeactivateLicenseCommand command)
    {
        command.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Heartbeat check (periodic online validation)</summary>
    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat([FromBody] HeartbeatCommand command)
    {
        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Online license validation</summary>
    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] ValidateLicenseCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>Renew a license (extends expiry, deducts balance)</summary>
    [HttpPost("renew")]
    [Authorize]
    public async Task<IActionResult> Renew([FromBody] RenewLicenseCommand command)
    {
        command.UserId = GetUserId();
        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Get current user's licenses</summary>
    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMyLicenses()
    {
        var result = await _mediator.Send(new GetMyLicensesQuery { UserId = GetUserId() });
        return Ok(result);
    }

    /// <summary>[Admin] Create/gift a license to a user</summary>
    [HttpPost("admin-create")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminCreate([FromBody] AdminCreateLicenseCommand command)
    {
        var result = await _mediator.Send(command);
        return result.Success ? Created("", result) : BadRequest(result);
    }

    /// <summary>[Admin] Get all licenses with filtering</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] GetAllLicensesQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>[Admin] Revoke a license</summary>
    [HttpPut("{id}/revoke")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Revoke(Guid id)
    {
        var result = await _mediator.Send(new RevokeLicenseCommand { LicenseId = id, Action = "revoke" });
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>[Admin] Suspend a license</summary>
    [HttpPut("{id}/suspend")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Suspend(Guid id)
    {
        var result = await _mediator.Send(new RevokeLicenseCommand { LicenseId = id, Action = "suspend" });
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>[Admin] Reinstate a license</summary>
    [HttpPut("{id}/reinstate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reinstate(Guid id)
    {
        var result = await _mediator.Send(new RevokeLicenseCommand { LicenseId = id, Action = "reinstate" });
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
