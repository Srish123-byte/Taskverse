using Taskverse.Business.DTOs;

namespace Taskverse.Api.Tests.Helpers;

public static partial class MockData
{
    public static UserDto GetUserDto(string userId = "user-123") => new()
    {
        UserId    = userId,
        FullName  = "John Doe",
        Email     = "john.doe@example.com",
        Phone     = "+911234567890",
        Role      = "Student",
        Status    = "ACTIVE",
        CreatedAt = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc),
        ModifiedAt = null
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
        PageSize   = 20
    };

    public static CreateUserDto GetCreateUserDto() => new()
    {
        FullName = "Jane Smith",
        Email    = "jane.smith@example.com",
        Phone    = "+919876543210",
        CollegeName = "Horizon Institute of Tech",
        Role     = "Student",
        Password = "SecurePass123!"
    };

    public static UpdateUserDto GetUpdateUserDto() => new()
    {
        FullName = "Updated Name",
        Phone    = null,
        Status   = null
    };
}
