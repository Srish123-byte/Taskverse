namespace Taskverse.API.College.Service.Models;

public record RegistrationCollegeRecord(
    string CollegeId,
    string Name);

public record RegistrationClassRecord(
    string ClassId,
    string CollegeId,
    string Name,
    string? AcademicYear);

public record RegistrationBatchRecord(
    string BatchId,
    string ClassId,
    string CollegeId,
    string Name);

public record CreateCollegeClassRequest(
    string Name,
    string? AcademicYear,
    string? Department);

public record CreateCollegeBatchRequest(
    string Name,
    string? Description,
    int? Capacity);

public record CollegeClassSummaryRecord(
    string ClassId,
    string CollegeId,
    string Name,
    string? AcademicYear,
    string? Department,
    int TotalStudents,
    int TotalCapacity,
    DateTime CreatedAt,
    List<CollegeBatchSummaryRecord> Batches);

public record CollegeBatchSummaryRecord(
    string BatchId,
    string ClassId,
    string CollegeId,
    string Name,
    string? Description,
    int Capacity,
    int StudentCount,
    DateTime CreatedAt);

public record PendingUserRecord(
    string UserId,
    string FullName,
    string Email,
    string Role,
    string Status,
    DateTime CreatedAt,
    string? InstitutionName);

public record CollegeRecord(
    Guid CollegeId,
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

public record CollegeSearchRequest(
    string? Query,
    string Status = "all");

public record CollegeSearchResultRecord(
    string CollegeId,
    string Name,
    string? City,
    string? State,
    string? AdminName,
    string? AdminEmail,
    int TotalUsers,
    string Status);
