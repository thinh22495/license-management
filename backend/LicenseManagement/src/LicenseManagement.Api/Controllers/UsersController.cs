using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.Users.Commands;
using LicenseManagement.Application.Users.DTOs;
using LicenseManagement.Application.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LicenseManagement.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
[SwaggerTag("Quản lý người dùng — chỉ dành cho Admin")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Lấy danh sách người dùng",
        Description = "Trả về danh sách người dùng có phân trang. Hỗ trợ tìm kiếm theo từ khóa, lọc theo vai trò và trạng thái khóa."
    )]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers([FromQuery] GetUsersQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost]
    [SwaggerOperation(
        Summary = "Tạo người dùng mới",
        Description = "Admin tạo tài khoản người dùng mới với email, số điện thoại, mật khẩu, họ tên và vai trò."
    )]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserCommand command)
    {
        var result = await _mediator.Send(command);
        return result.Success ? Created("", result) : BadRequest(result);
    }

    [HttpPut("{id}")]
    [SwaggerOperation(
        Summary = "Cập nhật thông tin người dùng",
        Description = "Cập nhật họ tên, số điện thoại hoặc ảnh đại diện của người dùng theo ID."
    )]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpPut("{id}/admin-update")]
    [SwaggerOperation(
        Summary = "Admin cập nhật thông tin người dùng",
        Description = "Admin cập nhật email, họ tên, số điện thoại, vai trò và trạng thái xác minh email của người dùng."
    )]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AdminUpdateUser(Guid id, [FromBody] AdminUpdateUserCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpPut("{id}/lock")]
    [SwaggerOperation(
        Summary = "Khóa hoặc mở khóa người dùng",
        Description = "Thay đổi trạng thái khóa của người dùng. Khi bị khóa, người dùng sẽ không thể đăng nhập."
    )]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> LockUser(Guid id, [FromBody] LockUserRequest request)
    {
        var result = await _mediator.Send(new LockUserCommand { UserId = id, IsLocked = request.IsLocked });
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpPost("{id}/topup")]
    [SwaggerOperation(
        Summary = "Nạp tiền vào tài khoản người dùng",
        Description = "Admin nạp thêm số dư vào tài khoản người dùng với số tiền và ghi chú tùy chọn."
    )]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
