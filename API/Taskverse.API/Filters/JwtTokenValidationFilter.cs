using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;

namespace Taskverse.Api.Filters
{
    public class JwtTokenValidationFilter : IAsyncActionFilter
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(JwtTokenValidationFilter));

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            try
            {
                var hasAllowAnonymous = context.ActionDescriptor.EndpointMetadata
                    .OfType<AllowAnonymousAttribute>()
                    .Any();

                if (hasAllowAnonymous)
                {
                    await next();
                    return;
                }

                var authorizationHeader = context.HttpContext.Request.Headers[HeaderNames.Authorization].ToString();

                if (string.IsNullOrWhiteSpace(authorizationHeader))
                {
                    _log.Error("JwtTokenValidationFilter: Authorization header is missing.");
                    context.Result = new UnauthorizedResult();
                    return;
                }

                var token = authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? authorizationHeader["Bearer ".Length..].Trim()
                    : authorizationHeader.Trim();

                var handler = new JwtSecurityTokenHandler();

                if (!handler.CanReadToken(token))
                {
                    _log.Error("JwtTokenValidationFilter: Authorization header does not contain a parseable JWT.");
                    context.Result = new UnauthorizedResult();
                    return;
                }

                await next();
            }
            catch (Exception ex)
            {
                _log.Error("JwtTokenValidationFilter: Unhandled exception during token validation.", ex);
                throw;
            }
        }
    }
}
