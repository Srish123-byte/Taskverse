using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Taskverse.Shared.Diagnostics;

public sealed record SystemInfoResponse(
    string AssemblyName,
    string AssemblyVersion,
    DateTime AssemblyDateUtc);

public static class SystemEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapSystemEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/system", () => Results.Ok(SystemInfoResponseFactory.Create()))
            .AllowAnonymous()
            .WithTags("System")
            .WithName("GetSystemInfo");

        return endpoints;
    }
}

internal static class SystemInfoResponseFactory
{
    public static SystemInfoResponse Create()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var assemblyName = assembly.GetName();

        return new SystemInfoResponse(
            assemblyName.Name ?? "Unknown",
            assemblyName.Version?.ToString() ?? "Unknown",
            ResolveAssemblyDateUtc(assembly));
    }

    private static DateTime ResolveAssemblyDateUtc(Assembly assembly)
    {
        if (string.IsNullOrWhiteSpace(assembly.Location) || !File.Exists(assembly.Location))
        {
            return DateTime.UtcNow;
        }

        return File.GetLastWriteTimeUtc(assembly.Location);
    }
}
