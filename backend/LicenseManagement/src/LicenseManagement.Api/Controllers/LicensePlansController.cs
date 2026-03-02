using LicenseManagement.Application.LicensePlans.Commands;
using LicenseManagement.Application.LicensePlans.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenseManagement.Api.Controllers;

[ApiController]
[Route("api/v1/products/{productId}/plans")]
public class LicensePlansController : ControllerBase
{
    private readonly IMediator _mediator;

    public LicensePlansController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetPlans(Guid productId, [FromQuery] bool? activeOnly)
    {
        var result = await _mediator.Send(new GetLicensePlansQuery { ProductId = productId, ActiveOnly = activeOnly });
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreatePlan(Guid productId, [FromBody] CreateLicensePlanCommand command)
    {
        command.ProductId = productId;
        var result = await _mediator.Send(command);
        return result.Success ? Created("", result) : BadRequest(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpdateLicensePlanCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeletePlan(Guid id)
    {
        var result = await _mediator.Send(new DeleteLicensePlanCommand { Id = id });
        return result.Success ? Ok(result) : NotFound(result);
    }
}
