using Microsoft.EntityFrameworkCore;
using Npgsql;
using Taskverse.API.CodingEngine.Service.Clients;
using Taskverse.API.CodingEngine.Service.Clients.Judge0;
using Taskverse.API.CodingEngine.Service.Managers;
using Taskverse.API.CodingEngine.Service.Models;
using Taskverse.API.CodingEngine.Service.Services;
using Taskverse.API.CodingEngine.Service.Workers;
using Taskverse.Data.DataAccess;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

// Database configuration
var connStr = builder.Configuration.GetConnectionString("TaskverseDb")
    ?? throw new InvalidOperationException("Connection string 'TaskverseDb' is missing.");

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connStr);
var dataSource = dataSourceBuilder.Build();

builder.Services.AddSingleton(dataSource);
builder.Services.AddDbContext<TaskverseContext>(options =>
    options.UseNpgsql(dataSource));

// Configuration options
builder.Services.Configure<ReportsServiceSettings>(builder.Configuration.GetSection("ReportsService"));
builder.Services.Configure<Judge0Settings>(builder.Configuration.GetSection("Judge0"));
builder.Services.Configure<CodingEngineWorkerOptions>(builder.Configuration.GetSection("CodingEngineWorkers"));
builder.Services.Configure<NodeHealthCheckSettings>(builder.Configuration.GetSection("NodeHealthCheck"));

// Dependency Injection
builder.Services.AddScoped<ICodingEngineManager, CodingEngineManager>();
builder.Services.AddScoped<IDispatchService, DispatchService>();
builder.Services.AddScoped<IPollService, PollService>();
builder.Services.AddScoped<IExecutionLifecycleService, ExecutionLifecycleService>();
builder.Services.AddSingleton<RateLimiterFactory>();

builder.Services.AddHttpClient<IReportsServiceClient, ReportsServiceClient>((serviceProvider, client) =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<ReportsServiceSettings>>().Value;
    if (string.IsNullOrWhiteSpace(settings.BaseUrl))
    {
        throw new InvalidOperationException("ReportsService:BaseUrl is missing.");
    }
    client.BaseAddress = new Uri(settings.BaseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds > 0 ? settings.TimeoutSeconds : 30);
});

builder.Services.AddHttpClient<IJudge0Client, Judge0Client>((serviceProvider, client) =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<Judge0Settings>>().Value;
    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds > 0 ? settings.TimeoutSeconds : 30);
    if (!string.IsNullOrWhiteSpace(settings.ApiKey))
    {
        client.DefaultRequestHeaders.Add(settings.ApiKeyHeaderName, settings.ApiKey);
    }
    if (!string.IsNullOrWhiteSpace(settings.ApiHost))
    {
        client.DefaultRequestHeaders.Add(settings.ApiHostHeaderName, settings.ApiHost);
    }
});

// Configure Workers dynamically similar to Startup.cs
var workerOptions = new CodingEngineWorkerOptions();
builder.Configuration.GetSection("CodingEngineWorkers").Bind(workerOptions);

if (workerOptions.Workers.Count == 0)
{
    workerOptions.Workers.Add(new WorkerSettings
    {
        WorkerId = "dispatch-default",
        WorkerType = "dispatch",
        PollingIntervalSeconds = 3,
        MaxConcurrentExecutions = 5,
        BatchSize = 10,
        RateLimitPerMinute = 100
    });
    workerOptions.Workers.Add(new WorkerSettings
    {
        WorkerId = "poll-default",
        WorkerType = "poll",
        PollingIntervalSeconds = 3,
        MaxConcurrentExecutions = 10,
        BatchSize = 20,
        RateLimitPerMinute = 200
    });
}

foreach (var worker in workerOptions.Workers)
{
    if (string.IsNullOrWhiteSpace(worker.WorkerId))
    {
        worker.WorkerId = Guid.NewGuid().ToString("N");
    }

    var capturedWorker = worker;
    var workerType = worker.WorkerType?.ToLowerInvariant() ?? "dispatch";

    switch (workerType)
    {
        case "poll":
            builder.Services.AddHostedService(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<PollWorker>>();
                var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<CodingEngineWorkerOptions>>();
                var rateLimiterFactory = sp.GetRequiredService<RateLimiterFactory>();
                var config = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?> { ["WorkerId"] = capturedWorker.WorkerId })
                    .Build();
                return new PollWorker(sp, logger, optionsMonitor, config, rateLimiterFactory);
            });
            break;

        default:
            builder.Services.AddHostedService(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<DispatchWorker>>();
                var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<CodingEngineWorkerOptions>>();
                var rateLimiterFactory = sp.GetRequiredService<RateLimiterFactory>();
                var config = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?> { ["WorkerId"] = capturedWorker.WorkerId })
                    .Build();
                return new DispatchWorker(sp, logger, optionsMonitor, config, rateLimiterFactory);
            });
            break;
    }
}

builder.Services.AddHostedService<NodeHealthCheckWorker>();

var host = builder.Build();
host.Run();
