namespace Taskverse.API.Assessments.Service.Models;

public class ProctorServiceSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}
