using log4net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Taskverse.API.CodingEngine.Service.Clients;
using Taskverse.API.CodingEngine.Service.Managers;
using Taskverse.API.CodingEngine.Service.Models;
using Taskverse.API.CodingEngine.Service.Orchestrators;
using Taskverse.Data.DataAccess;
using Taskverse.Shared.Diagnostics;

namespace Taskverse.API.CodingEngine.Service;

public class Startup
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(Startup));
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

        var logConfigPath = Path.Combine(
            environment.ContentRootPath,
            Configuration["Logging:Log4NetConfigFileRelativePath"] ?? "Log4Net.config");

        log4net.Config.XmlConfigurator.Configure(
            LogManager.GetRepository(System.Reflection.Assembly.GetEntryAssembly()!),
            new FileInfo(logConfigPath));

        Log.Info("Taskverse.API.CodingEngine.Service startup initialized.");
    }

    public void ConfigureServices(IServiceCollection services)
    {
        ConfigureMvc(services);
        ConfigureDatabase(services);
        ConfigureDependencyInjection(services);
        ConfigureOptions(services);
        ConfigureSwagger(services);
        ConfigureCors(services);
        services.AddHealthChecks();
    }

    public void Configure(WebApplication app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Taskverse CodingEngine Service v1");
            });
        }

        app.UseHttpsRedirection();
        app.MapControllers();
        app.MapSystemEndpoint();
        app.UseCors("AllowTaskverse");
        app.MapHealthChecks("/health");
    }

    private void ConfigureDatabase(IServiceCollection services)
    {
        var connStr = Configuration.GetConnectionString("TaskverseDb")
            ?? throw new InvalidOperationException("Connection string 'TaskverseDb' is missing.");

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connStr);
        var dataSource = dataSourceBuilder.Build();

        services.AddSingleton(dataSource);
        services.AddDbContext<TaskverseContext>(options =>
            options.UseNpgsql(dataSource));
    }

    private static void ConfigureDependencyInjection(IServiceCollection services)
    {
        services.AddScoped<ICodingEngineOrchestrator, CodingEngineOrchestrator>();
        services.AddScoped<ICodingEngineManager, CodingEngineManager>();
        services.AddHttpClient<IReportsServiceClient, ReportsServiceClient>((serviceProvider, client) =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<ReportsServiceSettings>>().Value;

            if (string.IsNullOrWhiteSpace(settings.BaseUrl))
            {
                throw new InvalidOperationException("ReportsService:BaseUrl is missing.");
            }

            client.BaseAddress = new Uri(settings.BaseUrl, UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds > 0 ? settings.TimeoutSeconds : 30);
        });
    }

    private void ConfigureOptions(IServiceCollection services)
    {
        services.Configure<ReportsServiceSettings>(Configuration.GetSection("ReportsService"));
    }

    private static void ConfigureSwagger(IServiceCollection services)
    {
        services.AddSwaggerGen();
    }

    private static void ConfigureMvc(IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
    }

    private static void ConfigureCors(IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowTaskverse", policy =>
            {
                policy.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });
    }
}
