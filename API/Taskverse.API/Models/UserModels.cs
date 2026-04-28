using System.ComponentModel.DataAnnotations;

namespace Taskverse.Api.Models;

public class UserResponseModel
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateUserRequestModel
{
    [Required] public string Email { get; set; } = string.Empty;
    [Required] public string FirstName { get; set; } = string.Empty;
    [Required] public string LastName { get; set; } = string.Empty;
    [Required] public string Role { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class UpdateUserRequestModel
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool? IsActive { get; set; }
}

public class UserSearchRequestModel
{
    public string? Email { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PagedUserResponseModel
{
    public List<UserResponseModel> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}
