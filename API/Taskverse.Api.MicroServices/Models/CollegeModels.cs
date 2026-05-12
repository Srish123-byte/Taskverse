namespace Taskverse.Api.MicroServices.Models;

public record CollegeModel(
    string CollegeId,
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

public record CollegeActionModel(
    string PerformedBy,
    string? Reason = null);
