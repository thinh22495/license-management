using System.Security.Claims;
using LicenseManagement.Application.Licenses.Commands;
using LicenseManagement.Application.Licenses.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LicenseManagement.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[SwaggerTag("License - Mua, kích hoạt, gia hạn, quản lý license")]
public class LicensesController : ControllerBase
{
    private readonly IMediator _mediator;

    public LicensesController(IMediator mediator) => _mediator = mediator;

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpPost("purchase")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Mua license",
        Description = "Người dùng mua license theo gói (LicensePlanId). Hệ thống sẽ trừ số dư tài khoản và tạo license key mới. Yêu cầu đăng nhập, UserId được lấy từ JWT token.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Purchase([FromBody] PurchaseLicenseCommand command)
    {
        command.UserId = GetUserId();
        var result = await _mediator.Send(command);
        return result.Success ? Created("", result) : BadRequest(result);
    }

    [HttpPost("activate")]
    [SwaggerOperation(
        Summary = "Kích hoạt license trên thiết bị",
        Description = "Client gọi API này để kích hoạt license trên một thiết bị cụ thể. Cần truyền LicenseKey và HardwareId của thiết bị. Hệ thống tự động lấy IpAddress từ request. Không yêu cầu đăng nhập.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Activate([FromBody] ActivateLicenseCommand command)
    {
        command.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("deactivate")]
    [SwaggerOperation(
        Summary = "Hủy kích hoạt license trên thiết bị",
        Description = "Hủy kích hoạt license trên một thiết bị cụ thể bằng LicenseKey và HardwareId. Hệ thống tự động lấy IpAddress từ request. Không yêu cầu đăng nhập.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Deactivate([FromBody] DeactivateLicenseCommand command)
    {
        command.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("heartbeat")]
    [SwaggerOperation(
        Summary = "Heartbeat - kiểm tra license định kỳ",
        Description = "Client gọi định kỳ để xác nhận license vẫn đang hoạt động trực tuyến. Cần truyền LicenseKey và HardwareId. Nếu license hết hạn hoặc bị thu hồi, server sẽ trả về lỗi. Không yêu cầu đăng nhập.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Heartbeat([FromBody] HeartbeatCommand command)
    {
        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("validate")]
    [SwaggerOperation(
        Summary = "Xác thực license trực tuyến",
        Description = "Kiểm tra tính hợp lệ của license theo LicenseKey và HardwareId. Trả về trạng thái license, thông tin sản phẩm, ngày hết hạn và các thông tin liên quan. Không yêu cầu đăng nhập.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> Validate([FromBody] ValidateLicenseCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("renew")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Gia hạn license",
        Description = "Gia hạn license đã hết hạn hoặc sắp hết hạn. Hệ thống sẽ trừ số dư tài khoản và kéo dài thời gian sử dụng. Yêu cầu đăng nhập, UserId được lấy từ JWT token.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Renew([FromBody] RenewLicenseCommand command)
    {
        command.UserId = GetUserId();
        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("redeem")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Đổi license key",
        Description = "Nhập license key chưa được gán cho ai để gán vào tài khoản hiện tại. Dùng cho trường hợp admin tạo license key trước và gửi cho người dùng. Yêu cầu đăng nhập, UserId được lấy từ JWT token.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Redeem([FromBody] RedeemLicenseCommand command)
    {
        command.UserId = GetUserId();
        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("my")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Lấy danh sách license của tôi",
        Description = "Lấy tất cả license thuộc về người dùng hiện tại. Bao gồm thông tin trạng thái, ngày hết hạn, sản phẩm và số lượng kích hoạt. Yêu cầu đăng nhập, UserId được lấy từ JWT token.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyLicenses()
    {
        var result = await _mediator.Send(new GetMyLicensesQuery { UserId = GetUserId() });
        return Ok(result);
    }

    [HttpGet("{id}/activations")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Lấy danh sách thiết bị đã kích hoạt",
        Description = "Lấy danh sách tất cả các thiết bị đã kích hoạt cho một license cụ thể. Người dùng phải là chủ sở hữu license. Trả về thông tin HardwareId, MachineName, IpAddress, thời gian kích hoạt. Yêu cầu đăng nhập.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetActivations(Guid id)
    {
        var result = await _mediator.Send(new GetLicenseActivationsQuery { LicenseId = id, UserId = GetUserId() });
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id}/activations/{activationId}")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Hủy kích hoạt thiết bị từ xa",
        Description = "Cho phép người dùng hủy kích hoạt một thiết bị từ xa mà không cần truy cập trực tiếp thiết bị đó. Người dùng phải là chủ sở hữu license. Yêu cầu đăng nhập.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoteDeactivate(Guid id, Guid activationId)
    {
        var result = await _mediator.Send(new RemoteDeactivateCommand
        {
            LicenseId = id,
            ActivationId = activationId,
            UserId = GetUserId()
        });
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("admin-create")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(
        Summary = "[Admin] Tạo license mới",
        Description = "Admin tạo license mới theo gói (LicensePlanId). Có thể gán cho một người dùng cụ thể (UserId) hoặc để trống để tạo license key chưa gán. Có thể thêm ghi chú (Note). Yêu cầu quyền Admin.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AdminCreate([FromBody] AdminCreateLicenseCommand command)
    {
        var result = await _mediator.Send(command);
        return result.Success ? Created("", result) : BadRequest(result);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(
        Summary = "[Admin] Lấy danh sách tất cả license",
        Description = "Lấy danh sách tất cả license trong hệ thống với hỗ trợ phân trang và lọc. Có thể lọc theo trạng thái (Status), sản phẩm (ProductId), hoặc tìm kiếm theo từ khóa (Search). Yêu cầu quyền Admin.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll([FromQuery] GetAllLicensesQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPut("{id}/revoke")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(
        Summary = "[Admin] Thu hồi license",
        Description = "Thu hồi vĩnh viễn một license. License bị thu hồi sẽ không thể sử dụng hoặc kích hoạt lại. Tất cả các thiết bị đang kích hoạt sẽ bị hủy kích hoạt. Yêu cầu quyền Admin.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Revoke(Guid id)
    {
        var result = await _mediator.Send(new RevokeLicenseCommand { LicenseId = id, Action = "revoke" });
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id}/suspend")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(
        Summary = "[Admin] Tạm ngưng license",
        Description = "Tạm ngưng một license. License bị tạm ngưng sẽ không thể sử dụng nhưng có thể được khôi phục lại bằng chức năng Reinstate. Yêu cầu quyền Admin.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Suspend(Guid id)
    {
        var result = await _mediator.Send(new RevokeLicenseCommand { LicenseId = id, Action = "suspend" });
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id}/reinstate")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(
        Summary = "[Admin] Khôi phục license",
        Description = "Khôi phục license đã bị tạm ngưng (Suspended) về trạng thái hoạt động bình thường. Chỉ áp dụng cho license bị tạm ngưng, không áp dụng cho license đã bị thu hồi. Yêu cầu quyền Admin.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Reinstate(Guid id)
    {
        var result = await _mediator.Send(new RevokeLicenseCommand { LicenseId = id, Action = "reinstate" });
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
