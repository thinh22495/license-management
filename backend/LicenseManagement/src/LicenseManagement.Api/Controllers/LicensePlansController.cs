using LicenseManagement.Application.LicensePlans.Commands;
using LicenseManagement.Application.LicensePlans.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LicenseManagement.Api.Controllers;

[ApiController]
[Route("api/v1/products/{productId}/plans")]
[SwaggerTag("Quản lý gói license — tạo, cập nhật, xoá và tra cứu các gói license theo sản phẩm")]
public class LicensePlansController : ControllerBase
{
    private readonly IMediator _mediator;

    public LicensePlansController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [SwaggerOperation(
        Summary = "Lấy danh sách gói license",
        Description = "Trả về danh sách các gói license của một sản phẩm. Có thể lọc chỉ các gói đang hoạt động bằng tham số activeOnly."
    )]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlans(Guid productId, [FromQuery] bool? activeOnly)
    {
        var result = await _mediator.Send(new GetLicensePlansQuery { ProductId = productId, ActiveOnly = activeOnly });
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(
        Summary = "Tạo gói license mới",
        Description = "Tạo một gói license mới cho sản phẩm. Yêu cầu quyền Admin. Cần cung cấp tên, thời hạn, số lượng kích hoạt tối đa, giá và tính năng."
    )]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreatePlan(Guid productId, [FromBody] CreateLicensePlanCommand command)
    {
        command.ProductId = productId;
        var result = await _mediator.Send(command);
        return result.Success ? Created("", result) : BadRequest(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(
        Summary = "Cập nhật gói license",
        Description = "Cập nhật thông tin gói license theo ID. Yêu cầu quyền Admin. Chỉ các trường được gửi mới được cập nhật."
    )]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpdateLicensePlanCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(
        Summary = "Xoá gói license",
        Description = "Xoá gói license theo ID. Yêu cầu quyền Admin. Trả về 404 nếu không tìm thấy gói license."
    )]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeletePlan(Guid id)
    {
        var result = await _mediator.Send(new DeleteLicensePlanCommand { Id = id });
        return result.Success ? Ok(result) : NotFound(result);
    }
}
