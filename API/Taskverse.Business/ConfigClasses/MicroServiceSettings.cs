namespace Taskverse.Business.ConfigClasses;

public class MicroServiceSettings
{
    public string BaseUrl { get; set; } = default!;
    public string BaseUrlDev { get; set; } = default!;
    public bool UseLocalMicroservices { get; set; }
    public int ServiceTimeoutSeconds { get; set; } = 30;
}
