using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Taskverse.Data.DataAccess
{
    public class TaskverseContextFactory : IDesignTimeDbContextFactory<TaskverseContext>
    {
        public TaskverseContext CreateDbContext(string[] args)
        {
            // Look for appsettings.json in the API project
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "../Taskverse.API");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("TaskverseDb");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'TaskverseDb' not found.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<TaskverseContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new TaskverseContext(optionsBuilder.Options);
        }
    }
}
