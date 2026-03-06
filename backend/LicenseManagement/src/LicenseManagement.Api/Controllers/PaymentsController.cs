using System.Security.Claims;
using LicenseManagement.Application.Payments.Commands;
using LicenseManagement.Application.Payments.DTOs;
using LicenseManagement.Application.Payments.Queries;
using LicenseManagement.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LicenseManagement.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[SwaggerTag("Quản lý thanh toán — nạp tiền qua cổng thanh toán và tra cứu lịch sử giao dịch")]
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

    [HttpPost("topup")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Tạo đơn nạp tiền",
        Description = "Tạo đơn nạp tiền và chuyển hướng người dùng đến cổng thanh toán (MoMo, VnPay, ZaloPay). Số tiền phải lớn hơn 0 và phương thức thanh toán không được là Balance.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

    [HttpPost("callback/momo")]
    [SwaggerOperation(
        Summary = "Callback IPN từ MoMo",
        Description = "Endpoint nhận thông báo thanh toán (IPN) từ MoMo. Hệ thống xác minh chữ ký và cập nhật trạng thái giao dịch. Không gọi trực tiếp — chỉ dành cho MoMo server gọi.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
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

    [HttpGet("callback/vnpay")]
    [SwaggerOperation(
        Summary = "Callback return URL từ VnPay",
        Description = "Endpoint nhận kết quả thanh toán từ VnPay qua query string. Hệ thống xác minh chữ ký SecureHash và chuyển hướng người dùng về trang kết quả trên frontend. Không gọi trực tiếp — chỉ dành cho VnPay redirect.")]
    [ProducesResponseType(StatusCodes.Status302Found)]
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

    [HttpPost("callback/zalopay")]
    [SwaggerOperation(
        Summary = "Callback từ ZaloPay",
        Description = "Endpoint nhận thông báo thanh toán từ ZaloPay. Hệ thống xác minh MAC và cập nhật trạng thái giao dịch. Không gọi trực tiếp — chỉ dành cho ZaloPay server gọi.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
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

    [HttpGet("transactions")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Lấy lịch sử giao dịch",
        Description = "Trả về danh sách giao dịch của người dùng hiện tại, hỗ trợ phân trang. Bao gồm các giao dịch nạp tiền, mua license và hoàn tiền.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
