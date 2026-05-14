using Microsoft.EntityFrameworkCore;
using Npgsql;
using Taskverse.Data.DataAccess;
using Taskverse.API.Users.Service.Services;

namespace Taskverse.API.Users.Service;

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
        ConfigureSwagger(services);
        ConfigureCors(services);
        ConfigureMvc(services);
        ConfigureDatabase(services);
        ConfigureDependencyInjection(services);

        services.AddHealthChecks();
    }

    public void Configure(WebApplication app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Taskverse Users Service v1");
            });
        }

        app.UseHttpsRedirection();
        app.UseCors("AllowTaskverse");

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

    private void ConfigureSwagger(IServiceCollection services)
    {
        services.AddSwaggerGen();
    }

    private void ConfigureDependencyInjection(IServiceCollection services)
    {
        services.AddScoped<IPendingUserService, PendingUserService>();
    }

    private void ConfigureMvc(IServiceCollection services)
    {
        services.AddControllers();
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
