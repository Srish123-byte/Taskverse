using Microsoft.EntityFrameworkCore;
using Taskverse.Data.DataAccess;

namespace Taskverse.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Configure Kestrel with longer timeout
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(60);
            options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(60);
        });
        
        var startup = new Startup(builder.Environment);
        startup.ConfigureServices(builder.Services);
        var app = builder.Build();

        // Apply pending migrations and create database if it doesn't exist
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TaskverseContext>();
            try
            {
                db.Database.Migrate();
            }
            catch (Exception ex)
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while migrating the database.");
            }
        }

        startup.Configure(app, app.Environment);
        app.Run();
    }
}
