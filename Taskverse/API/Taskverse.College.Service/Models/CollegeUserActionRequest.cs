namespace Taskverse.API.College.Service.Models;

public record CollegeUserActionRequest(
    string PerformedBy,
    Guid? PerformedByUserId,
    string? Reason);
