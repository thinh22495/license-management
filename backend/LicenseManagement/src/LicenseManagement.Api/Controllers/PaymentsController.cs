using System.Security.Claims;
using LicenseManagement.Application.Payments.Commands;
using LicenseManagement.Application.Payments.DTOs;
using LicenseManagement.Application.Payments.Queries;
using LicenseManagement.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenseManagement.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;

    public PaymentsController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _configuration = configuration;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>Create a top-up order (redirects to payment gateway)</summary>
    [HttpPost("topup")]
    [Authorize]
    public async Task<IActionResult> TopUp([FromBody] TopUpRequestDto request)
    {
        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var method) || method == PaymentMethod.Balance)
            return BadRequest(new { success = false, message = "Phương thức thanh toán không hợp lệ" });

        var baseUrl = _configuration["App:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";

        var command = new CreateTopUpCommand
        {
            UserId = GetUserId(),
            Amount = request.Amount,
            PaymentMethod = method,
            ReturnUrl = request.ReturnUrl,
            NotifyUrl = $"{baseUrl}/api/v1/payments/callback/{request.PaymentMethod.ToLowerInvariant()}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
        };

        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>MoMo IPN callback</summary>
    [HttpPost("callback/momo")]
    public async Task<IActionResult> MoMoCallback()
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();

        // MoMo sends signature in payload JSON
        var json = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(payload);
        var signature = json.GetProperty("signature").GetString() ?? "";

        var result = await _mediator.Send(new ProcessPaymentCallbackCommand
        {
            PaymentMethod = PaymentMethod.MoMo,
            Payload = payload,
            Signature = signature,
        });

        // MoMo expects specific response format
        return Ok(new { resultCode = result.Success ? 0 : 1, message = result.Message ?? "" });
    }

    /// <summary>VnPay return URL callback</summary>
    [HttpGet("callback/vnpay")]
    public async Task<IActionResult> VnPayCallback()
    {
        var queryString = Request.QueryString.Value ?? "";

        var result = await _mediator.Send(new ProcessPaymentCallbackCommand
        {
            PaymentMethod = PaymentMethod.VnPay,
            Payload = queryString,
            Signature = Request.Query["vnp_SecureHash"].ToString(),
        });

        // Redirect to frontend with result
        var frontendUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:3000";
        var status = result.Success ? "success" : "failed";
        return Redirect($"{frontendUrl}/topup/result?status={status}&message={Uri.EscapeDataString(result.Message ?? "")}");
    }

    /// <summary>ZaloPay callback</summary>
    [HttpPost("callback/zalopay")]
    public async Task<IActionResult> ZaloPayCallback()
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();

        var json = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(payload);
        var mac = json.GetProperty("mac").GetString() ?? "";

        var result = await _mediator.Send(new ProcessPaymentCallbackCommand
        {
            PaymentMethod = PaymentMethod.ZaloPay,
            Payload = payload,
            Signature = mac,
        });

        // ZaloPay expects specific response format
        return Ok(new { return_code = result.Success ? 1 : 2, return_message = result.Message ?? "" });
    }

    /// <summary>Get current user's transaction history</summary>
    [HttpGet("transactions")]
    [Authorize]
    public async Task<IActionResult> GetMyTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetMyTransactionsQuery
        {
            UserId = GetUserId(),
            Page = page,
            PageSize = pageSize,
        });
        return Ok(result);
    }
}
