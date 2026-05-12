using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;
using Taskverse.Auth.Service.Services;
using Taskverse.Data.DataAccess;

namespace Taskverse.Auth.Service;

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
        ConfigureAuthentication(services);
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
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Taskverse Authentication Service v1");
            });
        }

        app.UseHttpsRedirection();
        app.UseCors("AllowTaskverse");
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health");
    }

    private void ConfigureAuthentication(IServiceCollection services)
    {
        var jwtSettings = Configuration.GetSection("JwtSettings");
        var secret = jwtSettings["Key"] ?? jwtSettings["Secret"] 
            ?? throw new InvalidOperationException("JWT secret not configured");
        var key = Encoding.ASCII.GetBytes(secret);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
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

    private void ConfigureDependencyInjection(IServiceCollection services)
    {
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
    }

    private void ConfigureSwagger(IServiceCollection services)
    {
        services.AddSwaggerGen();
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
