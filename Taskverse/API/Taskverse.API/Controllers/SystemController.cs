using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Taskverse.Shared.Diagnostics;

namespace Taskverse.Api.Controllers;

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
    [AllowAnonymous]
    [SwaggerResponse(200, Type = typeof(SystemInfoResponse))]
    public IActionResult Get()
    {
        return Ok(SystemInfoResponseFactory.Create());
    }
}
