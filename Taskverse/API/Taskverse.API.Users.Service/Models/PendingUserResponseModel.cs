namespace Taskverse.API.Users.Service.Models;

public record PendingUserResponseModel(
    string UserId,
    string FullName,
    string Email,
    string Role,
    string Status,
    DateTime CreatedAt,
    string? InstitutionName);
