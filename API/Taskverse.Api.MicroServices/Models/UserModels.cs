namespace Taskverse.Api.MicroServices.Models;

public record UserModel(
    string UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateUserModel(
    string Email,
    string FirstName,
    string LastName,
    string Role,
    string Password);

public record UpdateUserModel(
    string? FirstName,
    string? LastName,
    bool? IsActive);

public record UserSearchCriteriaModel(
    string? Email = null,
    string? Role = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 20);

public record PagedUserResultModel(
    List<UserModel> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);
