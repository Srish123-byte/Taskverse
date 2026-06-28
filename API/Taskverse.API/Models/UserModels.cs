using Newtonsoft.Json;

namespace Taskverse.Api.Models;

public class UserResponseModel
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? EnrollmentNumber { get; set; }
    public Guid? CollegeId { get; set; }
    [JsonProperty("collegeName")]
    public string? CollegeName { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? BatchId { get; set; }
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
    public string? EnrollmentNumber { get; set; }
    public Guid? CollegeId { get; set; }
    [JsonProperty("collegeName")]
    public string? CollegeName { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? BatchId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}


public class RegistrationCollegeResponseModel
{
    public string CollegeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class RegistrationClassResponseModel
{
    public string ClassId { get; set; } = string.Empty;
    public string CollegeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
}

public class RegistrationBatchResponseModel
{
    public string BatchId { get; set; } = string.Empty;
    public string ClassId { get; set; } = string.Empty;
    public string CollegeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
