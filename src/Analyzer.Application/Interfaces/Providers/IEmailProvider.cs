namespace Analyzer.Application.Interfaces.Providers;

public interface IEmailProvider
{
    Task SendEmailAsync(string toEmail, string subject, string body);
}