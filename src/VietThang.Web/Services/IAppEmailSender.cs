namespace VietThang.Web.Services;

/// <summary>Gửi email (xác nhận đơn, thông báo trạng thái). Bản hiện tại chỉ ghi log; tuần 13 thay bằng MailKit/SMTP.</summary>
public interface IAppEmailSender
{
    Task SendAsync(string? to, string subject, string htmlBody);
}

public class LogEmailSender : IAppEmailSender
{
    private readonly ILogger<LogEmailSender> _logger;
    public LogEmailSender(ILogger<LogEmailSender> logger) => _logger = logger;

    public Task SendAsync(string? to, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(to)) return Task.CompletedTask;
        _logger.LogInformation("[EMAIL] To: {To} | Subject: {Subject}\n{Body}", to, subject, htmlBody);
        return Task.CompletedTask;
    }
}
