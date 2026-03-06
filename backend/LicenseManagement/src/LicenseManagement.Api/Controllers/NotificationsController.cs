using System.Security.Claims;
using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Notifications.Commands;
using LicenseManagement.Application.Notifications.Queries;
using LicenseManagement.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LicenseManagement.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[SwaggerTag("Quản lý thông báo — xem, đánh dấu đã đọc, xoá và nhận thông báo realtime qua SSE")]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ISseNotificationService _sseService;

    public NotificationsController(IMediator mediator, ISseNotificationService sseService)
    {
        _mediator = mediator;
        _sseService = sseService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpGet]
    [Authorize]
    [SwaggerOperation(
        Summary = "Lấy danh sách thông báo",
        Description = "Trả về danh sách thông báo của người dùng hiện tại. Có thể lọc chỉ thông báo chưa đọc và giới hạn số lượng kết quả trả về.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetNotifications([FromQuery] bool unreadOnly = false, [FromQuery] int limit = 50)
    {
        var result = await _mediator.Send(new GetMyNotificationsQuery
        {
            UserId = GetUserId(),
            UnreadOnly = unreadOnly,
            Limit = limit,
        });
        return Ok(result);
    }

    [HttpGet("unread-count")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Lấy số thông báo chưa đọc",
        Description = "Trả về tổng số thông báo chưa đọc của người dùng hiện tại. Dùng để hiển thị badge trên biểu tượng chuông thông báo.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUnreadCount()
    {
        var result = await _mediator.Send(new GetUnreadCountQuery { UserId = GetUserId() });
        return Ok(result);
    }

    [HttpPut("read")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Đánh dấu thông báo đã đọc",
        Description = "Đánh dấu một thông báo cụ thể là đã đọc (truyền NotificationId) hoặc đánh dấu tất cả thông báo là đã đọc (để NotificationId là null).")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkRead([FromBody] MarkReadRequest request)
    {
        var result = await _mediator.Send(new MarkNotificationReadCommand
        {
            UserId = GetUserId(),
            NotificationId = request.NotificationId,
        });
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Xoá thông báo",
        Description = "Xoá một thông báo theo ID. Chỉ chủ sở hữu thông báo mới có quyền xoá.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteNotificationCommand
        {
            UserId = GetUserId(),
            NotificationId = id,
        });
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("stream")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Kết nối SSE nhận thông báo realtime",
        Description = "Mở kết nối Server-Sent Events (SSE) để nhận thông báo realtime. Kết nối sẽ gửi heartbeat mỗi 30 giây để duy trì. Phản hồi có Content-Type: text/event-stream.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task Stream(CancellationToken ct)
    {
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        var userId = GetUserId();
        var writer = new StreamWriter(Response.Body);

        // Send initial connection event
        await writer.WriteLineAsync("event: connected");
        await writer.WriteLineAsync($"data: {{\"userId\":\"{userId}\"}}");
        await writer.WriteLineAsync();
        await writer.FlushAsync(ct);

        _sseService.AddClient(userId, writer);

        try
        {
            // Keep connection alive with periodic heartbeats
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(30_000, ct); // heartbeat every 30s
                await writer.WriteLineAsync(": heartbeat");
                await writer.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _sseService.RemoveClient(userId, writer);
        }
    }

    [HttpPost("send")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(
        Summary = "[Admin] Gửi thông báo",
        Description = "Gửi thông báo đến một người dùng cụ thể (truyền UserId) hoặc broadcast đến tất cả người dùng (để UserId là null). Hỗ trợ nhiều kênh gửi: web, email. Chỉ Admin mới có quyền sử dụng.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Send([FromBody] SendNotificationRequest request)
    {
        var result = await _mediator.Send(new SendNotificationCommand
        {
            UserId = request.UserId,
            Title = request.Title,
            Body = request.Body,
            Type = request.Type,
            Channels = request.Channels,
        });
        return result.Success ? Ok(result) : BadRequest(result);
    }
}

public class MarkReadRequest
{
    public Guid? NotificationId { get; set; }
}

public class SendNotificationRequest
{
    public Guid? UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public string[] Channels { get; set; } = ["web"];
}
