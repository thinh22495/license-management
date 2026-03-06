using LicenseManagement.Application.Dashboard.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LicenseManagement.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
[SwaggerTag("Bảng điều khiển quản trị — thống kê tổng quan và dữ liệu biểu đồ dành cho Admin")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator) => _mediator = mediator;

    [HttpGet("stats")]
    [SwaggerOperation(
        Summary = "Lấy thống kê tổng quan",
        Description = "Trả về các số liệu thống kê tổng quan của hệ thống: tổng số người dùng, tổng số license, doanh thu, số license sắp hết hạn, v.v. Chỉ Admin mới có quyền truy cập.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStats()
    {
        var result = await _mediator.Send(new GetDashboardStatsQuery());
        return Ok(result);
    }

    [HttpGet("charts")]
    [SwaggerOperation(
        Summary = "Lấy dữ liệu biểu đồ",
        Description = "Trả về dữ liệu biểu đồ trong N ngày gần nhất (mặc định 30 ngày). Bao gồm dữ liệu doanh thu theo ngày, số lượng đăng ký mới, số license được kích hoạt, v.v. Chỉ Admin mới có quyền truy cập.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCharts([FromQuery] int days = 30)
    {
        var result = await _mediator.Send(new GetChartsDataQuery { Days = days });
        return Ok(result);
    }
}
