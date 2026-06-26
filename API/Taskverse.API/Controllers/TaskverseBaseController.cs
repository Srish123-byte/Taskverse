using Microsoft.AspNetCore.Mvc;
using Taskverse.Api.Filters;

namespace Taskverse.Api.Controllers;

/// <summary>
/// Base controller for all Taskverse API controllers.
/// Applies JWT token validation and provides common header accessors.
/// </summary>
[ServiceFilter(typeof(JwtTokenValidationFilter))]
public abstract class TaskverseBaseController : Controller
{
    private const string UserIdHeaderKey = "UserId";
    private const string UserRoleHeaderKey = "UserRole";
    private const string CollegeIdHeaderKey = "CollegeId";

    /// <summary>
    /// Gets the UserId from the request header. Throws if missing.
    /// </summary>
    protected string UserId
    {
        get
        {
            var value = Request?.Headers[UserIdHeaderKey].ToString();
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException("UserId header is missing or invalid");
            return value;
        }
    }

    /// <summary>
    /// Gets the UserRole from the request header. Returns empty string if missing.
    /// </summary>
    protected string UserRole =>
        Request?.Headers[UserRoleHeaderKey].ToString() ?? string.Empty;

    /// <summary>
    /// Gets the CollegeId from the request header. Returns empty string if missing.
    /// </summary>
    protected string CollegeId =>
        Request?.Headers[CollegeIdHeaderKey].ToString() ?? string.Empty;
}
