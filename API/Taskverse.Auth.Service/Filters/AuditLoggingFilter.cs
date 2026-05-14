using log4net;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Taskverse.API.Auth.Service.Filters;

public sealed class AuditLoggingFilter : IAsyncActionFilter
{
    private static readonly ILog AuditLog = LogManager.GetLogger(typeof(AuditLoggingFilter));

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executedContext = await next();

        var httpContext = context.HttpContext;
        var request = httpContext.Request;
        var statusCode = executedContext.HttpContext.Response.StatusCode;
        var controllerAction = context.ActionDescriptor as ControllerActionDescriptor;
        var actionName = controllerAction is null
            ? context.ActionDescriptor.DisplayName ?? "UnknownAction"
            : $"{controllerAction.ControllerName}.{controllerAction.ActionName}";
        var route = request.Path.HasValue ? request.Path.Value! : "/";
        var method = request.Method;
        var ipAddress = ResolveIpAddress(httpContext);
        var userId = ResolveUserId(context);
        var correlationId = ResolveCorrelationId(request);
        var outcome = executedContext.Exception is null || executedContext.ExceptionHandled
            ? "Success"
            : "Failure";

        AuditLog.Info(
            $"AUDIT action={actionName} method={method} route={route} status={statusCode} outcome={outcome} " +
            $"userId={userId ?? "anonymous"} ip={ipAddress ?? "unknown"} correlationId={correlationId ?? "n/a"}");
    }

    private static string? ResolveUserId(ActionExecutingContext context)
    {
        var claimUserId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(claimUserId))
            return claimUserId;

        if (TryGetStringValue(context, "UserId", out var userId))
            return userId;

        if (TryGetStringValue(context, "Email", out var email))
            return email;

        return null;
    }

    private static bool TryGetStringValue(ActionExecutingContext context, string propertyName, out string? value)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
                continue;

            var property = argument.GetType().GetProperty(propertyName);
            if (property is null)
                continue;

            var rawValue = property.GetValue(argument)?.ToString();
            if (!string.IsNullOrWhiteSpace(rawValue))
            {
                value = rawValue;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static string? ResolveCorrelationId(HttpRequest request)
    {
        if (request.Headers.TryGetValue("X-CorrelationId", out var correlationId))
            return correlationId.ToString();

        return request.HttpContext.TraceIdentifier;
    }

    private static string? ResolveIpAddress(HttpContext httpContext)
    {
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
            return forwardedFor.Split(',')[0].Trim();

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }
}
