using System.Net;
using System.Net.Mail;

namespace Microplex.Web.Services;

public sealed class EmailSender(IConfiguration configuration)
{
    public async Task SendAsync(string toAddress, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var host = configuration["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException("Email is not configured. Set Smtp:Host, Smtp:Port, Smtp:Username, Smtp:Password, and Smtp:FromAddress (user secrets or environment variables).");

        var port = int.TryParse(configuration["Smtp:Port"], out var parsedPort) ? parsedPort : 587;
        var username = configuration["Smtp:Username"];
        var password = configuration["Smtp:Password"];
        var fromAddress = configuration["Smtp:FromAddress"] ?? username
            ?? throw new InvalidOperationException("Smtp:FromAddress is not configured.");
        var fromName = configuration["Smtp:FromName"] ?? "Microplex Corporation";
        var enableSsl = !bool.TryParse(configuration["Smtp:EnableSsl"], out var configuredSsl) || configuredSsl;

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl
        };
        if (!string.IsNullOrWhiteSpace(username))
            client.Credentials = new NetworkCredential(username, password);

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toAddress);

        await client.SendMailAsync(message, cancellationToken);
    }
}
