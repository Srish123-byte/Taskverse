namespace Taskverse.Business.Interface;

public interface IEmailService
{
    Task SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public class EmailMessage
{
    public string ToAddress { get; set; } = string.Empty;
    public List<string> ToAddresses { get; set; } = new();
    public string? ToName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public List<EmailAttachment> Attachments { get; set; } = new();
}

public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
}
