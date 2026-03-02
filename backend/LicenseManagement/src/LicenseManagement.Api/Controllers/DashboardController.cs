using LicenseManagement.Application.Dashboard.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenseManagement.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator) => _mediator = mediator;

    /// <summary>Get dashboard summary stats</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var result = await _mediator.Send(new GetDashboardStatsQuery());
        return Ok(result);
    }

    /// <summary>Get charts data for the last N days</summary>
    [HttpGet("charts")]
    public async Task<IActionResult> GetCharts([FromQuery] int days = 30)
    {
        var result = await _mediator.Send(new GetChartsDataQuery { Days = days });
        return Ok(result);
    }
}
