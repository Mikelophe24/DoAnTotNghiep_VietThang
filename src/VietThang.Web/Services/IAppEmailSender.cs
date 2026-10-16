using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace VietThang.Web.Services;

/// <summary>Gửi email: xác nhận đơn, thông báo trạng thái, đặt lại mật khẩu.</summary>
public interface IAppEmailSender
{
    Task SendAsync(string? to, string subject, string htmlBody);
}

/// <summary>Dùng khi chưa cấu hình SMTP: ghi nội dung email ra log để vẫn kiểm thử được luồng (ví dụ lấy link đặt lại mật khẩu).</summary>
public class LogEmailSender : IAppEmailSender
{
    private readonly ILogger<LogEmailSender> _logger;
    public LogEmailSender(ILogger<LogEmailSender> logger) => _logger = logger;

    public Task SendAsync(string? to, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(to)) return Task.CompletedTask;
        _logger.LogInformation("[EMAIL - chưa cấu hình SMTP, chỉ ghi log] To: {To} | Subject: {Subject}\n{Body}", to, subject, htmlBody);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Gửi email thật qua SMTP bằng MailKit. Cấu hình mục "Smtp" (Host, Port, User, Password, FromEmail, FromName).
/// Với Gmail: Host smtp.gmail.com, Port 587, User là địa chỉ Gmail, Password là "App password" 16 ký tự.
/// </summary>
public class SmtpEmailSender : IAppEmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string? to, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(to)) return;
        var s = _config.GetSection("Smtp");
        var host = s["Host"]!;
        var port = int.TryParse(s["Port"], out var p) ? p : 587;
        var user = s["User"];
        var fromEmail = string.IsNullOrWhiteSpace(s["FromEmail"]) ? user : s["FromEmail"];

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(s["FromName"] ?? "Thời trang Việt Thắng", fromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);
        if (!string.IsNullOrWhiteSpace(user)) await client.AuthenticateAsync(user, s["Password"]);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
        _logger.LogInformation("Đã gửi email \"{Subject}\" tới {To}", subject, to);
    }
}
