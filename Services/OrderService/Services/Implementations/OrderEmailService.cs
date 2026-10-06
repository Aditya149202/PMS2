using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace OrderService.Services;


public class OrderEmailService : IOrderEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<OrderEmailService> _logger;

    public OrderEmailService(IConfiguration config, ILogger<OrderEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendOrderConfirmationAsync(string? toEmail, string doctorName, int orderId, decimal totalAmount)
    {
        if (string.IsNullOrWhiteSpace(toEmail) || !MailboxAddress.TryParse(toEmail, out var to))
        {
            _logger.LogWarning("Order {OrderId} confirmation email skipped: no valid recipient address.", orderId);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_config["Smtp:FromName"] ?? "PMS", _config["Smtp:FromAddress"]!));
            message.To.Add(to);
            message.Subject = $"Order #{orderId} confirmed";
            message.Body = new TextPart("plain")
            {
                Text = $"Hi {doctorName},\n\nYour payment was received and order #{orderId} has been placed.\n" +
                       $"Total: {totalAmount:N2}\n\nYou will be able to pick it up once it has been verified.\n"
            };

            using var client = new SmtpClient { Timeout = 10_000 }; // an SMTP outage must not hang payment confirmation
            await client.ConnectAsync(_config["Smtp:Host"], int.Parse(_config["Smtp:Port"]!), SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_config["Smtp:Username"]!, _config["Smtp:Password"]!);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send confirmation email for order {OrderId}.", orderId);
        }
    }
}