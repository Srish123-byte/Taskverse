using log4net;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Taskverse.API.Reports.Service.Managers;
using Taskverse.API.Reports.Service.Orchestrators;
using Taskverse.API.Reports.Service.Services;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.Reports.Service;

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

        Log.Info("Taskverse.API.Reports.Service startup initialized.");
    }

    public void ConfigureServices(IServiceCollection services)
    {
        ConfigureMvc(services);
        ConfigureDatabase(services);
        ConfigureDependencyInjection(services);
        ConfigureSwagger(services);
        services.AddHealthChecks();
    }

    public void Configure(WebApplication app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Taskverse Reports Service v1");
            });
        }

        // app.UseHttpsRedirection(); // Disabled for local dev to avoid SSL cert issues
        app.MapControllers();
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
        services.AddScoped<IResultOrchestrator, ResultOrchestrator>();
        services.AddScoped<IAttemptEvaluationService, AttemptEvaluationService>();
        services.AddScoped<IResultManager, ResultManager>();
        services.AddScoped<IResultEvaluationStrategyFactory, ResultEvaluationStrategyFactory>();
        services.AddScoped<IResultEvaluationStrategy, McqResultEvaluationStrategy>();
        services.AddScoped<IResultEvaluationStrategy, CodingResultEvaluationStrategy>();
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        services.AddScoped<IReportExportManager, ReportExportManager>();

        // Enterprise Reports
        services.AddScoped<IEnterpriseReportsManager, EnterpriseReportsManager>();
        services.AddScoped<IEnterpriseReportsOrchestrator, EnterpriseReportsOrchestrator>();
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
}

