namespace Taskverse.College.Service;

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
        services.AddControllers();
        services.AddHealthChecks();
    }

    public void Configure(WebApplication app, IWebHostEnvironment env)
    {
        app.UseHttpsRedirection();
        MapCollegeEndpoints(app);
        app.MapHealthChecks("/health");
    }

    private void MapCollegeEndpoints(WebApplication app)
    {
        app.MapGet("/api/colleges", () => Results.Ok(CollegeStore.Colleges))
            .WithName("GetColleges")
            .WithOpenApi();

        app.MapGet("/api/colleges/pending", () =>
            Results.Ok(CollegeStore.Colleges.Where(college => college.ApprovalStatus == ApprovalStatuses.Pending).ToList()))
            .WithName("GetPendingColleges")
            .WithOpenApi();

        app.MapGet("/api/colleges/{id:guid}", (Guid id) =>
        {
            var college = CollegeStore.Colleges.FirstOrDefault(item => item.CollegeId == id);
            return college is null ? Results.NotFound() : Results.Ok(college);
        })
            .WithName("GetCollegeById")
            .WithOpenApi();

        app.MapPost("/api/colleges/{id:guid}/approve", (Guid id, CollegeActionRequest request) =>
        {
            var college = CollegeStore.Colleges.FirstOrDefault(item => item.CollegeId == id);
            if (college is null) return Results.NotFound();

            var updated = college with
            {
                ApprovalStatus = ApprovalStatuses.Approved,
                Status = CollegeStatuses.Active,
                IsActive = true,
                ApprovedAt = DateTime.UtcNow,
                ApprovedBy = request.PerformedBy,
                Notes = request.Reason
            };

            CollegeStore.Replace(updated);
            return Results.Ok(updated);
        })
            .WithName("ApproveCollege")
            .WithOpenApi();

        app.MapPost("/api/colleges/{id:guid}/reject", (Guid id, CollegeActionRequest request) =>
        {
            var college = CollegeStore.Colleges.FirstOrDefault(item => item.CollegeId == id);
            if (college is null) return Results.NotFound();

            var updated = college with
            {
                ApprovalStatus = ApprovalStatuses.Rejected,
                Status = CollegeStatuses.Rejected,
                IsActive = false,
                ApprovedAt = null,
                ApprovedBy = request.PerformedBy,
                Notes = request.Reason
            };

            CollegeStore.Replace(updated);
            return Results.Ok(updated);
        })
            .WithName("RejectCollege")
            .WithOpenApi();

        app.MapPost("/api/colleges/{id:guid}/deactivate", (Guid id, CollegeActionRequest request) =>
        {
            var college = CollegeStore.Colleges.FirstOrDefault(item => item.CollegeId == id);
            if (college is null) return Results.NotFound();

            var updated = college with
            {
                Status = CollegeStatuses.Inactive,
                IsActive = false,
                Notes = request.Reason,
                ApprovedBy = request.PerformedBy
            };

            CollegeStore.Replace(updated);
            return Results.Ok(updated);
        })
            .WithName("DeactivateCollege")
            .WithOpenApi();

        app.MapPost("/api/colleges/{id:guid}/reactivate", (Guid id, CollegeActionRequest request) =>
        {
            var college = CollegeStore.Colleges.FirstOrDefault(item => item.CollegeId == id);
            if (college is null) return Results.NotFound();

            var updated = college with
            {
                Status = CollegeStatuses.Active,
                IsActive = true,
                Notes = request.Reason,
                ApprovedBy = request.PerformedBy
            };

            CollegeStore.Replace(updated);
            return Results.Ok(updated);
        })
            .WithName("ReactivateCollege")
            .WithOpenApi();
    }
}
