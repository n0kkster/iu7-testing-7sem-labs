namespace Analyzer.Infrastructure.Providers;

using System.Net.Mail;
using Analyzer.Application.Interfaces.Providers;
using Microsoft.Extensions.Configuration;

public class SmtpEmailProvider(IConfiguration config) : IEmailProvider
{
    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var host = config["SmtpSettings:Host"] ?? "localhost";
        var port = int.TryParse(config["SmtpSettings:Port"], out var parsedPort) ? parsedPort : 1025;
        var from = config["SmtpSettings:From"] ?? "noreply@analyzer.local";

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = false,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        using var message = new MailMessage(from, toEmail, subject, body);
        await client.SendMailAsync(message);
    }
}