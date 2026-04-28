namespace Taskverse.Api.Configuration;

public class CorsSettings
{
    public string[] OriginUrls { get; set; } = [];
    public string[] ExposedHeaders { get; set; } = [];
    public string[] AllowedMethods { get; set; } = [];
    public string[] AllowedHeaders { get; set; } = [];
    public bool AllowCredentials { get; set; } = true;
}
