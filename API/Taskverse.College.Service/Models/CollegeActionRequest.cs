namespace Taskverse.API.College.Service.Models;

public record CollegeActionRequest(
    string PerformedBy,
    string? Reason);
