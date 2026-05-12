namespace Taskverse.API.Proctor.Service;

public class Startup
{
    private readonly IConfigurationBuilder _builder;

    public IConfigurationRoot Configuration { get; }

    public Startup(IWebHostEnvironment environment)
    {
        _builder = new ConfigurationBuilder()
            .SetBasePath(environment.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        Configuration = _builder.Build();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddHealthChecks();
    }

    public void Configure(WebApplication app, IWebHostEnvironment env)
    {
        app.UseHttpsRedirection();
        app.MapControllers();
        app.MapHealthChecks("/health");
    }
}
