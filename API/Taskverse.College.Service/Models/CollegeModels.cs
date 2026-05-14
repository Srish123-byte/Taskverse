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

public record CollegeRecord(
    Guid CollegeId,
    string Name,
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
