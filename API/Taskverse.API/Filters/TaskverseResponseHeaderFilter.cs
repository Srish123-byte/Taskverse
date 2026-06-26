using Microsoft.AspNetCore.Mvc.Filters;

namespace Taskverse.Api.Filters
{
    public class TaskverseResponseHeaderFilter : IActionFilter
    {
        public static readonly string ResponseHeaderName = "TaskverseApiResponse";

        public void OnActionExecuting(ActionExecutingContext context)
        {
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            context.HttpContext.Response.Headers[ResponseHeaderName] = "1";
            context.HttpContext.Response.Headers["Cache-Control"] = "no-cache, no-store";
            context.HttpContext.Response.Headers["Pragma"] = "no-cache";
            context.HttpContext.Response.Headers["Content-Security-Policy"] = "default-src 'self'";
            context.HttpContext.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            context.HttpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.HttpContext.Response.Headers["X-Frame-Options"] = "DENY";
        }
    }
}
