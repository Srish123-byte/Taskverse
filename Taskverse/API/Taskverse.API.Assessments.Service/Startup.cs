using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Taskverse.API.Assessments.Service.Clients;
using Taskverse.API.Assessments.Service.Managers;
using Taskverse.API.Assessments.Service.Models;
using Taskverse.API.Assessments.Service.Orchestrators;
using Taskverse.API.Assessments.Service.Services;
using Taskverse.Data.DataAccess;
using Taskverse.Shared.Diagnostics;

namespace Taskverse.API.Assessments.Service;

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
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Taskverse Assessments Service v1");
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
        services.AddScoped<IQuestionManager, QuestionManager>();
        services.AddScoped<IAssessmentManager, AssessmentManager>();
        services.AddScoped<IAssessmentOrchestrator, AssessmentOrchestrator>();
        services.AddScoped<IStudentAttemptAnswerSaveStrategyFactory, StudentAttemptAnswerSaveStrategyFactory>();
        services.AddScoped<IStudentAttemptAnswerSaveStrategy, ObjectiveStudentAttemptAnswerSaveStrategy>();
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
        services.AddHttpClient<IProctorServiceClient, ProctorServiceClient>((serviceProvider, client) =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<ProctorServiceSettings>>().Value;

            if (string.IsNullOrWhiteSpace(settings.BaseUrl))
            {
                throw new InvalidOperationException("ProctorService:BaseUrl is missing.");
            }

            client.BaseAddress = new Uri(settings.BaseUrl, UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds > 0 ? settings.TimeoutSeconds : 30);
        });
        services.AddScoped<IWhatsAppNotificationService, WhatsAppNotificationService>();
        services.AddHostedService<AssessmentStatusTransitionService>();
    }

    private void ConfigureOptions(IServiceCollection services)
    {
        services.Configure<AssessmentSettings>(Configuration.GetSection("AssessmentSettings"));
        services.Configure<AssessmentStatusTransitionSettings>(Configuration.GetSection("AssessmentStatusTransition"));
        services.Configure<ReportsServiceSettings>(Configuration.GetSection("ReportsService"));
        services.Configure<ProctorServiceSettings>(Configuration.GetSection("ProctorService"));
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
