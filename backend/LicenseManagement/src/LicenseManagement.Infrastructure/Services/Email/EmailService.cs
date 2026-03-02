using LicenseManagement.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace LicenseManagement.Infrastructure.Services.Email;

public class SmtpSettings
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "noreply@licensemanagement.com";
    public string FromName { get; set; } = "License Management";
    public bool UseSsl { get; set; } = true;
}

public class EmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _settings = configuration.GetSection("Email:Smtp").Get<SmtpSettings>() ?? new SmtpSettings();
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, _settings.UseSsl, ct);

            if (!string.IsNullOrEmpty(_settings.Username))
                await client.AuthenticateAsync(_settings.Username, _settings.Password, ct);

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}: {Subject}", to, subject);
        }
    }

    public async Task SendTemplateAsync(string to, string templateName, Dictionary<string, string> variables, CancellationToken ct = default)
    {
        var (subject, body) = GetTemplate(templateName, variables);
        await SendAsync(to, subject, body, ct);
    }

    private static (string subject, string body) GetTemplate(string templateName, Dictionary<string, string> variables)
    {
        var (subject, template) = templateName switch
        {
            "license_purchased" => (
                "Mua license thành công",
                """
                <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto">
                    <h2 style="color:#1677ff">Mua License Thành Công!</h2>
                    <p>Xin chào <strong>{{UserName}}</strong>,</p>
                    <p>Bạn đã mua thành công gói license:</p>
                    <table style="border-collapse:collapse;width:100%">
                        <tr><td style="padding:8px;border:1px solid #ddd"><strong>Sản phẩm</strong></td><td style="padding:8px;border:1px solid #ddd">{{ProductName}}</td></tr>
                        <tr><td style="padding:8px;border:1px solid #ddd"><strong>Gói</strong></td><td style="padding:8px;border:1px solid #ddd">{{PlanName}}</td></tr>
                        <tr><td style="padding:8px;border:1px solid #ddd"><strong>License Key</strong></td><td style="padding:8px;border:1px solid #ddd"><code>{{LicenseKey}}</code></td></tr>
                        <tr><td style="padding:8px;border:1px solid #ddd"><strong>Hết hạn</strong></td><td style="padding:8px;border:1px solid #ddd">{{ExpiresAt}}</td></tr>
                    </table>
                    <p style="margin-top:16px;color:#666">License Management System</p>
                </div>
                """
            ),
            "topup_success" => (
                "Nạp tiền thành công",
                """
                <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto">
                    <h2 style="color:#52c41a">Nạp Tiền Thành Công!</h2>
                    <p>Xin chào <strong>{{UserName}}</strong>,</p>
                    <p>Tài khoản của bạn đã được nạp <strong>{{Amount}}</strong>.</p>
                    <p>Số dư hiện tại: <strong>{{Balance}}</strong></p>
                    <p style="margin-top:16px;color:#666">License Management System</p>
                </div>
                """
            ),
            "license_expiring" => (
                "License sắp hết hạn",
                """
                <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto">
                    <h2 style="color:#faad14">License Sắp Hết Hạn!</h2>
                    <p>Xin chào <strong>{{UserName}}</strong>,</p>
                    <p>License <strong>{{ProductName}} - {{PlanName}}</strong> của bạn sẽ hết hạn vào <strong>{{ExpiresAt}}</strong>.</p>
                    <p>Hãy gia hạn sớm để tránh gián đoạn sử dụng.</p>
                    <p style="margin-top:16px;color:#666">License Management System</p>
                </div>
                """
            ),
            "license_expired" => (
                "License đã hết hạn",
                """
                <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto">
                    <h2 style="color:#ff4d4f">License Đã Hết Hạn</h2>
                    <p>Xin chào <strong>{{UserName}}</strong>,</p>
                    <p>License <strong>{{ProductName}} - {{PlanName}}</strong> đã hết hạn.</p>
                    <p>Vui lòng gia hạn hoặc mua gói mới để tiếp tục sử dụng.</p>
                    <p style="margin-top:16px;color:#666">License Management System</p>
                </div>
                """
            ),
            _ => ($"Thông báo từ License Management", $"<p>{string.Join("", variables.Select(kv => $"{kv.Key}: {kv.Value}<br/>"))}</p>"),
        };

        foreach (var (key, value) in variables)
        {
            template = template.Replace($"{{{{{key}}}}}", value);
        }

        return (subject, template);
    }
}
