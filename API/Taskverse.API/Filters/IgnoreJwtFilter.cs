namespace Taskverse.Api.Filters;

/// <summary>
/// Marker attribute to skip JWT token validation for specific actions.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class IgnoreJwtFilter : Attribute
{
}
