namespace Taskverse.Api.Models;

public class UserResponseModel
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Guid? CollegeId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

public class CreateUserRequestModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Guid? CollegeId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UpdateUserRequestModel
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public Guid? CollegeId { get; set; }
    public Guid? BatchId { get; set; }
    public Guid? ClassId { get; set; }
    public string? Status { get; set; }
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
    public List<UserResponseModel> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}
