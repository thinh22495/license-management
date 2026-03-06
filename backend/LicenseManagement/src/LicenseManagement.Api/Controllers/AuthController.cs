using LicenseManagement.Application.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LicenseManagement.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[SwaggerTag("Xác thực - Đăng ký, đăng nhập, refresh token, đăng xuất")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    [SwaggerOperation(
        Summary = "Đăng ký tài khoản",
        Description = "Tạo tài khoản mới với Email, Số điện thoại, Mật khẩu và Họ tên. Trả về access token và refresh token (lưu trong cookie HttpOnly).")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);

        // Set refresh token as HttpOnly cookie
        SetRefreshTokenCookie(result.Data!.RefreshToken);
        return Ok(result);
    }

    [HttpPost("login")]
    [SwaggerOperation(
        Summary = "Đăng nhập",
        Description = "Đăng nhập bằng Email và Mật khẩu. Trả về access token và refresh token (lưu trong cookie HttpOnly).")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);

        SetRefreshTokenCookie(result.Data!.RefreshToken);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [SwaggerOperation(
        Summary = "Làm mới token",
        Description = "Sử dụng refresh token từ cookie để tạo access token và refresh token mới. Không cần truyền body.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
            return BadRequest(new { Success = false, Message = "Refresh token not found" });

        var result = await _mediator.Send(new RefreshTokenCommand { RefreshToken = refreshToken });
        if (!result.Success) return Unauthorized(result);

        SetRefreshTokenCookie(result.Data!.RefreshToken);
        return Ok(result);
    }

    [HttpPost("logout")]
    [SwaggerOperation(
        Summary = "Đăng xuất",
        Description = "Xóa refresh token khỏi cookie và đăng xuất người dùng.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("refreshToken");
        return Ok(new { Success = true, Message = "Logged out successfully" });
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        };
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
}
