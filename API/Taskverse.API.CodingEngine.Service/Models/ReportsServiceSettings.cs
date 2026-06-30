namespace Taskverse.API.CodingEngine.Service.Models;

public class ReportsServiceSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}

public class Judge0Settings
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiHost { get; set; } = string.Empty;
    public string ApiKeyHeaderName { get; set; } = "X-RapidAPI-Key";
    public string ApiHostHeaderName { get; set; } = "X-RapidAPI-Host";
}
