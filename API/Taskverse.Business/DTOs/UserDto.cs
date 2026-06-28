using Taskverse.Data.Enums;

namespace Taskverse.Business.DTOs;

public class UserDto
{
    public string UserId { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Phone { get; set; }
    public string? EnrollmentNumber { get; set; }
    public Guid? CollegeId { get; set; }
    public string? CollegeName { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? BatchId { get; set; }
    public string Role { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

public class CreateUserDto
{
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Phone { get; set; }
    public string? EnrollmentNumber { get; set; }
    public Guid? CollegeId { get; set; }
    public string? CollegeName { get; set; }
    public string Role { get; set; } = default!;
    public string Password { get; set; } = default!;
    public UserStatus Status { get; set; }
    public Guid? BatchId { get; set; }
    public Guid? ClassId { get; set; }
}

public class UpdateUserDto
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public Guid? CollegeId { get; set; }
    public Guid? BatchId { get; set; }
    public Guid? ClassId { get; set; }
    public string? Status { get; set; }
}

public class PagedUserDto
{
    public List<UserDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class RegistrationCollegeDto
{
    public string CollegeId { get; set; } = default!;
    public string Name { get; set; } = default!;
}

public class RegistrationClassDto
{
    public string ClassId { get; set; } = default!;
    public string CollegeId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? AcademicYear { get; set; }
}

public class RegistrationBatchDto
{
    public string BatchId { get; set; } = default!;
    public string ClassId { get; set; } = default!;
    public string CollegeId { get; set; } = default!;
    public string Name { get; set; } = default!;
}
