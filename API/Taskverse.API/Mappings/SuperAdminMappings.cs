using Taskverse.Api.Models;
using Taskverse.Business.DTOs;

namespace Taskverse.Api.Mappings;

public static class SuperAdminMappings
{
    public static CollegeActionDto ToDto(this CollegeActionRequestModel model, string performedBy) => new()
    {
        PerformedBy = performedBy,
        Reason = model.Reason
    };

    public static CollegeResponseModel ToResponseModel(this CollegeDto dto) => new()
    {
        CollegeId = dto.CollegeId,
        Name = dto.Name,
        City = dto.City,
        State = dto.State,
        Status = dto.Status,
        ApprovalStatus = dto.ApprovalStatus,
        IsActive = dto.IsActive,
        RequestedAt = dto.RequestedAt,
        RequestedBy = dto.RequestedBy,
        ApprovedAt = dto.ApprovedAt,
        ApprovedBy = dto.ApprovedBy,
        Notes = dto.Notes
    };

    public static SuperAdminDashboardResponseModel ToResponseModel(this SuperAdminDashboardDto dto) => new()
    {
        Totals = new SuperAdminTotalsResponseModel
        {
            ActiveColleges = dto.Totals.ActiveColleges,
            RegisteredStudents = dto.Totals.RegisteredStudents,
            AssessmentsThisMonth = dto.Totals.AssessmentsThisMonth,
            AssessmentsPreviousMonth = dto.Totals.AssessmentsPreviousMonth
        },
        PendingApprovals = dto.PendingApprovals.Select(x => x.ToResponseModel()).ToList(),
        PlatformHealth = new PlatformHealthResponseModel
        {
            UptimePercent = dto.PlatformHealth.UptimePercent,
            ErrorRatePercent = dto.PlatformHealth.ErrorRatePercent,
            ApiStatus = dto.PlatformHealth.ApiStatus
        },
        RecentActivity = dto.RecentActivity.Select(x => new RecentActivityResponseModel
        {
            Action = x.Action,
            EntityType = x.EntityType,
            EntityId = x.EntityId,
            PerformedBy = x.PerformedBy,
            OccurredAt = x.OccurredAt,
            Details = x.Details
        }).ToList(),
        AverageScoresByCollege = dto.AverageScoresByCollege.Select(x => new CollegeScoreSummaryResponseModel
        {
            CollegeId = x.CollegeId,
            CollegeName = x.CollegeName,
            AverageScore = x.AverageScore,
            StudentsAssessed = x.StudentsAssessed
        }).ToList(),
        UsageTrends = dto.UsageTrends.Select(x => new UsageTrendPointResponseModel
        {
            Date = x.Date,
            Assessments = x.Assessments,
            StudentsAssessed = x.StudentsAssessed
        }).ToList()
    };
}
