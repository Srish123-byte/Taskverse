using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Taskverse.Data.DataAccess;

namespace Taskverse.Api.Filters
{
    public class JwtTokenValidationFilter : IAsyncActionFilter
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(JwtTokenValidationFilter));
        private static readonly TimeSpan InactivityTimeout = TimeSpan.FromMinutes(30);
        private readonly IConfiguration _configuration;
        private readonly TaskverseContext _context;

        public JwtTokenValidationFilter(IConfiguration configuration, TaskverseContext context)
        {
            _configuration = configuration;
            _context = context;
        }

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

                var principal = ValidateToken(token);
                if (principal is null)
                {
                    _log.Error("JwtTokenValidationFilter: Authorization header does not contain a valid JWT.");
                    context.Result = new UnauthorizedResult();
                    return;
                }

                var sessionIdClaim = principal.FindFirstValue("sid");
                if (!Guid.TryParse(sessionIdClaim, out var sessionId))
                {
                    _log.Error("JwtTokenValidationFilter: Session id claim is missing.");
                    context.Result = new UnauthorizedResult();
                    return;
                }

                var authSession = await _context.AuthSessions
                    .FirstOrDefaultAsync(session => session.AuthSessionId == sessionId && session.RevokedAt == null);
                if (authSession is null)
                {
                    _log.Error("JwtTokenValidationFilter: Auth session not found or revoked.");
                    context.Result = new UnauthorizedResult();
                    return;
                }

                var now = DateTime.UtcNow;
                if (now - authSession.LastActivityAt > InactivityTimeout)
                {
                    authSession.RevokedAt = now;
                    authSession.ModifiedAt = now;
                    await _context.SaveChangesAsync();
                    _log.Error("JwtTokenValidationFilter: Session expired due to inactivity.");
                    context.Result = new UnauthorizedResult();
                    return;
                }

                authSession.LastActivityAt = now;
                authSession.ModifiedAt = now;
                await _context.SaveChangesAsync();
                context.HttpContext.User = principal;

                await next();
            }
            catch (Exception ex)
            {
                _log.Error("JwtTokenValidationFilter: Unhandled exception during token validation.", ex);
                throw;
            }
        }

        private ClaimsPrincipal? ValidateToken(string token)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secret = jwtSettings["Key"] ?? jwtSettings["Secret"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                _log.Error("JwtTokenValidationFilter: JWT secret missing.");
                return null;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            if (!tokenHandler.CanReadToken(token))
            {
                return null;
            }

            try
            {
                return tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out _);
            }
            catch (Exception ex)
            {
                _log.Error("JwtTokenValidationFilter: JWT validation failed.", ex);
                return null;
            }
        }
    }
}
