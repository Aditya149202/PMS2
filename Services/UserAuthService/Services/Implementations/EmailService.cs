using UserAuthService.Services.Interfaces;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendSignUpConfirmationAsync(string toEmail, string userName)
    {
        var message = BuildMessage(toEmail, "Welcome to Pharmacy Management System",
            $"Hi {userName}, your account has been created successfully.");
        await SendAsync(message);
    }

    public async Task SendPasswordResetAsync(string toEmail, string rawResetToken)
    {
        // rawResetToken goes in a link the client builds, e.g. {frontendUrl}/reset?token={rawResetToken}
        var message = BuildMessage(toEmail, "Password Reset Request",
            $"Use this token to reset your password: {rawResetToken}");
        await SendAsync(message);
    }

    private MimeMessage BuildMessage(string toEmail, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_config["Smtp:FromAddress"]!));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };
        return message;
    }

    private async Task SendAsync(MimeMessage message)
    {
        using var client = new SmtpClient();
        await client.ConnectAsync(_config["Smtp:Host"], int.Parse(_config["Smtp:Port"]!), SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_config["Smtp:Username"]!, _config["Smtp:Password"]!);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}