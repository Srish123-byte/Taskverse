namespace Taskverse.Api.MicroServices.Models;

public record CollegeModel(
    string CollegeId,
    string Name,
    string? AdminName,
    string? City,
    string? State,
    string Status,
    string ApprovalStatus,
    bool IsActive,
    DateTime RequestedAt,
    string? RequestedBy,
    DateTime? ApprovedAt,
    string? ApprovedBy,
    string? Notes);

public record CollegeActionModel(
    string PerformedBy,
    string? Reason = null);

public record CollegeSearchModel(
    string? Query,
    string Status = "all");

public record CollegeSearchResultModel(
    string CollegeId,
    string Name,
    string? City,
    string? State,
    string? AdminName,
    string? AdminEmail,
    int TotalUsers,
    string Status);

public record RegistrationCollegeModel(
    string CollegeId,
    string Name);

public record RegistrationClassModel(
    string ClassId,
    string CollegeId,
    string Name,
    string? AcademicYear);

public record RegistrationBatchModel(
    string BatchId,
    string ClassId,
    string CollegeId,
    string Name);

public record CreateCollegeClassModel(
    string Name,
    string? AcademicYear,
    string? Department);

public record CreateCollegeBatchModel(
    string Name,
    string? Description,
    int? Capacity,
    string? SubjectId,
    string? SubjectName);

public record CollegeUserActionModel(
    string PerformedBy,
    Guid? PerformedByUserId,
    string? Reason);

public record CollegeClassSummaryModel(
    string ClassId,
    string CollegeId,
    string Name,
    string? AcademicYear,
    string? Department,
    int TotalStudents,
    int TotalCapacity,
    DateTime CreatedAt,
    List<CollegeBatchSummaryModel> Batches);

public record CollegeBatchSummaryModel(
    string BatchId,
    string ClassId,
    string CollegeId,
    string Name,
    string? Description,
    string? SubjectId,
    string? SubjectName,
    int Capacity,
    int StudentCount,
    DateTime CreatedAt,
    List<ApprovedTrainerModel> AssignedTrainers);

public record SubjectOptionModel(
    string SubjectId,
    string SubjectName);

public record ApprovedTrainerModel(
    string TrainerId,
    string UserId,
    string FullName,
    string Email);

public record AssignBatchTrainersModel(
    List<string> TrainerIds);
