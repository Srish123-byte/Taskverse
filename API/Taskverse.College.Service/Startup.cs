using log4net;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Taskverse.API.College.Service.Orchestrators;
using Taskverse.API.College.Service.Filters;
using Taskverse.API.College.Service.Services;
using Taskverse.Data.DataAccess;
using Taskverse.Shared.Diagnostics;

namespace Taskverse.API.College.Service;

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

        Log.Info("Taskverse.API.College.Service startup initialized.");
    }

    public void ConfigureServices(IServiceCollection services)
    {
        ConfigureMvc(services);
        ConfigureDatabase(services);
        ConfigureDependencyInjection(services);
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
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Taskverse Colleges Service v1");
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

    private void ConfigureSwagger(IServiceCollection services)
    {
        services.AddSwaggerGen();
    }

    private void ConfigureDependencyInjection(IServiceCollection services)
    {
        services.AddScoped<ICollegeOrchestrator, CollegeOrchestrator>();
        services.AddScoped<ICollegeService, CollegeService>();
        services.AddScoped<IAttendanceExportService, AttendanceExportService>();
    }

    private void ConfigureMvc(IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<AuditLoggingFilter>();
        });
        services.AddEndpointsApiExplorer();
    }

    private void ConfigureCors(IServiceCollection services)
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
