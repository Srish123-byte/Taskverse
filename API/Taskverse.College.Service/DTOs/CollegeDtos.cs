namespace Taskverse.API.College.Service.DTOs;

public class CreateCollegeClassDto
{
    public string Name { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
    public string? Department { get; set; }
}

public class CreateCollegeBatchDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? Capacity { get; set; }
}

public class CollegeClassSummaryDto
{
    public string ClassId { get; set; } = string.Empty;
    public string CollegeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
    public string? Department { get; set; }
    public int TotalStudents { get; set; }
    public int TotalCapacity { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CollegeBatchSummaryDto> Batches { get; set; } = [];
}

public class CollegeBatchSummaryDto
{
    public string BatchId { get; set; } = string.Empty;
    public string ClassId { get; set; } = string.Empty;
    public string CollegeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Capacity { get; set; }
    public int StudentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ApprovedTrainerDto> AssignedTrainers { get; set; } = [];
}

public class ApprovedTrainerDto
{
    public string TrainerId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class AssignBatchTrainersDto
{
    public List<string> TrainerIds { get; set; } = [];
}

public class CollegeUserActionDto
{
    public string PerformedBy { get; set; } = string.Empty;
    public Guid? PerformedByUserId { get; set; }
    public string? Reason { get; set; }
}

public class PendingUserDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? InstitutionName { get; set; }
}
