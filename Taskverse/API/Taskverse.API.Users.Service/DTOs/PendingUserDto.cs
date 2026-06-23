namespace Taskverse.API.Users.Service.DTOs;

public record PendingUserDto(
    string UserId,
    string FullName,
    string Email,
    string Role,
    string Status,
    DateTime CreatedAt,
    string? InstitutionName);
