using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Taskverse.Business.Configuration;
using Taskverse.Business.Interface;

namespace Taskverse.Business.Services;

public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public SmtpEmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ValidateSettings();

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(_settings.FromAddress, _settings.FromName),
            Subject = message.Subject,
            Body = message.HtmlBody,
            IsBodyHtml = true
        };

        var recipients = message.ToAddresses
            .Concat(string.IsNullOrWhiteSpace(message.ToAddress) ? [] : [message.ToAddress])
            .Select(address => address.Trim())
            .Where(address => !string.IsNullOrWhiteSpace(address))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (recipients.Count == 0)
        {
            throw new InvalidOperationException("At least one recipient email address is required.");
        }

        foreach (var recipient in recipients)
        {
            mailMessage.To.Add(new MailAddress(recipient, recipients.Count == 1 ? message.ToName : null));
        }

        if (message.Attachments != null)
        {
            foreach (var attachment in message.Attachments)
            {
                var memoryStream = new MemoryStream(attachment.Content);
                var mailAttachment = new Attachment(memoryStream, attachment.FileName, attachment.ContentType);
                mailMessage.Attachments.Add(mailAttachment);
            }
        }

        using var smtpClient = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password)
        };

        cancellationToken.ThrowIfCancellationRequested();
        await smtpClient.SendMailAsync(mailMessage);
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.Host) ||
            string.IsNullOrWhiteSpace(_settings.Username) ||
            string.IsNullOrWhiteSpace(_settings.Password) ||
            string.IsNullOrWhiteSpace(_settings.FromAddress))
        {
            throw new InvalidOperationException("EmailSettings must be configured before sending bulk upload emails.");
        }
    }
}
