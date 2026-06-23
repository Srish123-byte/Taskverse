using Microsoft.EntityFrameworkCore;
using Npgsql;
using Taskverse.API.CodingEngine.Worker.Service.Clients;
using Taskverse.API.CodingEngine.Worker.Service.Services;
using Taskverse.API.CodingEngine.Worker.Service.Workers;
using Taskverse.Data.DataAccess;

var builder = WebApplication.CreateBuilder(args);

var connStr = builder.Configuration.GetConnectionString("TaskverseDb")
    ?? throw new InvalidOperationException("Connection string 'TaskverseDb' is missing.");

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connStr);
var dataSource = dataSourceBuilder.Build();

builder.Services.AddSingleton(dataSource);
builder.Services.AddDbContext<TaskverseContext>(options =>
    options.UseNpgsql(dataSource));

builder.Services.AddScoped<ICodeExecutionService, CodeExecutionService>();
builder.Services.AddHttpClient<IReportsServiceClient, ReportsServiceClient>(client =>
{
    var baseUrl = builder.Configuration["ReportsService:BaseUrl"];
    if (string.IsNullOrWhiteSpace(baseUrl))
    {
        throw new InvalidOperationException("ReportsService:BaseUrl is missing.");
    }

    client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(300);
});

builder.Services.AddHostedService<CodeExecutionWorker>();

var app = builder.Build();
app.Run();
