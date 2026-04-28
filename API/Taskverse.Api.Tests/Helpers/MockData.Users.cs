using Taskverse.Business.DTOs;

namespace Taskverse.Api.Tests.Helpers;

public static partial class MockData
{
    public static UserDto GetUserDto(string userId = "user-123") => new()
    {
        UserId = userId,
        Email = "john.doe@example.com",
        FirstName = "John",
        LastName = "Doe",
        Role = "Student",
        IsActive = true,
        CreatedAt = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = null
    };

    public static PagedUserDto GetPagedUserDto() => new()
    {
        Items =
        [
            GetUserDto("user-123"),
            GetUserDto("user-456")
        ],
        TotalCount = 2,
        PageNumber = 1,
        PageSize = 20
    };

    public static CreateUserDto GetCreateUserDto() => new()
    {
        Email = "jane.smith@example.com",
        FirstName = "Jane",
        LastName = "Smith",
        Role = "Student",
        Password = "SecurePass123!"
    };

    public static UpdateUserDto GetUpdateUserDto() => new()
    {
        FirstName = "Updated",
        LastName = null,
        IsActive = null
    };
}
