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
    string? Status = null,
    string? Role = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10);

public record PagedPendingUserResultModel(
    List<PendingUserModel> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);

public record PendingUserModel(
    string UserId,
    string FullName,
    string Email,
    string Role,
    string Status,
    DateTime CreatedAt,
    string? InstitutionName);
