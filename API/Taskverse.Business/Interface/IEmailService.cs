namespace Taskverse.Business.Interface;

public interface IEmailService
{
    Task SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public class EmailMessage
{
    public List<EmailRecipient> ToAddresses { get; set; } = [];
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public List<EmailAttachment> Attachments { get; set; } = [];
}

public class EmailRecipient
{
    public string Address { get; set; } = string.Empty;
    public string? Name { get; set; }
}

public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public byte[] Content { get; set; } = [];
}
