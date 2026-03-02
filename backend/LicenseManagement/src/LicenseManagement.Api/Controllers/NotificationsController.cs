using System.Security.Claims;
using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Notifications.Commands;
using LicenseManagement.Application.Notifications.Queries;
using LicenseManagement.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenseManagement.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
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

    /// <summary>Get my notifications</summary>
    [HttpGet]
    [Authorize]
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

    /// <summary>Get unread count</summary>
    [HttpGet("unread-count")]
    [Authorize]
    public async Task<IActionResult> GetUnreadCount()
    {
        var result = await _mediator.Send(new GetUnreadCountQuery { UserId = GetUserId() });
        return Ok(result);
    }

    /// <summary>Mark notification(s) as read</summary>
    [HttpPut("read")]
    [Authorize]
    public async Task<IActionResult> MarkRead([FromBody] MarkReadRequest request)
    {
        var result = await _mediator.Send(new MarkNotificationReadCommand
        {
            UserId = GetUserId(),
            NotificationId = request.NotificationId,
        });
        return Ok(result);
    }

    /// <summary>SSE stream for realtime notifications</summary>
    [HttpGet("stream")]
    [Authorize]
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

    /// <summary>[Admin] Send notification to user or broadcast</summary>
    [HttpPost("send")]
    [Authorize(Roles = "Admin")]
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
