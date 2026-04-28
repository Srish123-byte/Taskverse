using System.Net.Mime;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Taskverse.Api.Controllers;

public record SystemModel
{
    public DateTime SystemTime { get; init; }
    public string? Title { get; init; }
    public string? Copyright { get; init; }
    public string? Description { get; init; }
    public string? Version { get; init; }
    public string? FullName { get; init; }
}

[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class SystemController : Controller
{
    // TODO: Remove this temporary endpoint once the API is ready
    [AllowAnonymous]
    [HttpGet("status")]
    [SwaggerResponse(200, Type = typeof(string))]
    public IActionResult GetStatus()
    {
        return Ok("API is under construction");
    }

    [HttpGet]
    [SwaggerResponse(200, Type = typeof(SystemModel))]
    public IActionResult Get()
    {
        var assembly = Assembly.GetEntryAssembly();
        var model = new SystemModel
        {
            SystemTime = DateTime.UtcNow,
            Title = assembly?.GetCustomAttribute<AssemblyTitleAttribute>()?.Title,
            Copyright = assembly?.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright,
            Description = assembly?.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description,
            Version = assembly?.GetName().Version?.ToString(),
            FullName = assembly?.GetName().FullName
        };

        return Ok(model);
    }
}
