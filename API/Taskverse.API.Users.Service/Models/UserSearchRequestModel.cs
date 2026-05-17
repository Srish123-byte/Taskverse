namespace Taskverse.API.Users.Service.Models;

public record UserSearchRequestModel(
    string? Status = null,
    string? Role = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10);
