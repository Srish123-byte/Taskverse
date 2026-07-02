namespace Taskverse.API.Models
{
    public class SendReportEmailRequest
    {
        public List<string> Recipients { get; set; } = [];
        public string FileName { get; set; } = "report.xlsx";
        public string FileContentBase64 { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string? Body { get; set; }
    }
}
