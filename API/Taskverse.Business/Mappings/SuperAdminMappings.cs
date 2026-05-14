using Taskverse.Api.MicroServices.Models;
using Taskverse.Business.DTOs;
using Taskverse.Data.DataAccess;

namespace Taskverse.Business.Mappings;

public static class SuperAdminMappings
{
    public static CollegeDto ToDto(this CollegeModel model) => new()
    {
        CollegeId = model.CollegeId,
        Name = model.Name,
        City = model.City,
        State = model.State,
        Status = model.Status,
        ApprovalStatus = model.ApprovalStatus,
        IsActive = model.IsActive,
        RequestedAt = model.RequestedAt,
        RequestedBy = model.RequestedBy,
        ApprovedAt = model.ApprovedAt,
        ApprovedBy = model.ApprovedBy,
        Notes = model.Notes
    };

    public static CollegeSearchDto ToDto(this CollegeSearchModel model) => new()
    {
        Query = model.Query,
        Status = model.Status
    };

    public static CollegeSearchResultDto ToDto(this CollegeSearchResultModel model) => new()
    {
        CollegeId = model.CollegeId,
        Name = model.Name,
        City = model.City,
        State = model.State,
        AdminName = model.AdminName,
        AdminEmail = model.AdminEmail,
        TotalUsers = model.TotalUsers,
        Status = model.Status
    };

    public static CollegeActionModel ToMicroServiceModel(this CollegeActionDto dto) =>
        new(dto.PerformedBy, dto.Reason);

    public static CollegeSearchModel ToMicroServiceModel(this CollegeSearchDto dto) =>
        new(dto.Query, dto.Status);

    public static PendingUserDto ToDto(this PendingUserModel model) => new()
    {
        UserId = model.UserId,
        FullName = model.FullName,
        Email = model.Email,
        Role = model.Role,
        Status = model.Status,
        CreatedAt = model.CreatedAt,
        InstitutionName = model.InstitutionName
    };

    public static RecentActivityDto ToDto(this AuditLog auditLog, string performedBy) => new()
    {
        Action = auditLog.Action,
        EntityType = auditLog.EntityType,
        EntityId = auditLog.EntityId?.ToString(),
        PerformedBy = performedBy,
        OccurredAt = auditLog.OccurredAt,
        Details = auditLog.Details
    };
}
