using Taskverse.Api.Models;
using Taskverse.Business.DTOs;

namespace Taskverse.Api.Mappings;

public static class CollegeAdminMappings
{
    public static CollegeAdminDashboardResponseModel ToResponseModel(this CollegeAdminDashboardDto dto) => new()
    {
        Totals = new CollegeAdminTotalsResponseModel
        {
            RegisteredStudents = dto.Totals.RegisteredStudents,
            RegisteredTrainers = dto.Totals.RegisteredTrainers,
            PendingApprovals = dto.Totals.PendingApprovals,
            TotalAssessments = dto.Totals.TotalAssessments,
            AssessmentsThisMonth = dto.Totals.AssessmentsThisMonth,
            AssessmentsPreviousMonth = dto.Totals.AssessmentsPreviousMonth
        },
        PendingApprovals = dto.PendingApprovals.Select(x => x.ToResponseModel()).ToList(),
        RecentActivity = dto.RecentActivity.Select(x => new RecentActivityResponseModel
        {
            Action = x.Action,
            EntityType = x.EntityType,
            EntityId = x.EntityId,
            PerformedBy = x.PerformedBy,
            OccurredAt = x.OccurredAt,
            Details = x.Details
        }).ToList(),
        UsageTrends = dto.UsageTrends.Select(x => new UsageTrendPointResponseModel
        {
            Date = x.Date,
            Assessments = x.Assessments,
            StudentsAssessed = x.StudentsAssessed
        }).ToList()
    };

    public static ClassConfigurationResponseModel ToResponseModel(this ClassConfigurationDto dto) => new()
    {
        Totals = new ClassConfigurationTotalsResponseModel
        {
            TotalClasses = dto.Totals.TotalClasses,
            TotalBatches = dto.Totals.TotalBatches,
            TotalStudents = dto.Totals.TotalStudents,
            CapacityUtilization = dto.Totals.CapacityUtilization
        },
        Classes = dto.Classes.Select(x => x.ToResponseModel()).ToList()
    };

    public static CollegeClassSummaryResponseModel ToResponseModel(this CollegeClassSummaryDto dto) => new()
    {
        ClassId = dto.ClassId,
        CollegeId = dto.CollegeId,
        Name = dto.Name,
        AcademicYear = dto.AcademicYear,
        Department = dto.Department,
        TotalStudents = dto.TotalStudents,
        TotalCapacity = dto.TotalCapacity,
        CreatedAt = dto.CreatedAt,
        Batches = dto.Batches.Select(x => x.ToResponseModel()).ToList()
    };

    public static CollegeBatchSummaryResponseModel ToResponseModel(this CollegeBatchSummaryDto dto) => new()
    {
        BatchId = dto.BatchId,
        ClassId = dto.ClassId,
        CollegeId = dto.CollegeId,
        Name = dto.Name,
        Description = dto.Description,
        SubjectId = dto.SubjectId,
        SubjectName = dto.SubjectName,
        Capacity = dto.Capacity,
        StudentCount = dto.StudentCount,
        CreatedAt = dto.CreatedAt,
        AssignedTrainers = dto.AssignedTrainers.Select(x => x.ToResponseModel()).ToList(),
        AssignedStudents = dto.AssignedStudents.Select(x => x.ToResponseModel()).ToList()
    };

    public static SubjectOptionResponseModel ToResponseModel(this SubjectOptionDto dto) => new()
    {
        SubjectId = dto.SubjectId,
        SubjectName = dto.SubjectName
    };

    public static ApprovedTrainerResponseModel ToResponseModel(this ApprovedTrainerDto dto) => new()
    {
        TrainerId = dto.TrainerId,
        UserId = dto.UserId,
        FullName = dto.FullName,
        Email = dto.Email
    };

    public static ApprovedStudentResponseModel ToResponseModel(this ApprovedStudentDto dto) => new()
    {
        StudentId = dto.StudentId,
        UserId = dto.UserId,
        FullName = dto.FullName,
        Email = dto.Email
    };

    public static CreateCollegeClassDto ToDto(this CreateCollegeClassRequestModel model) => new()
    {
        Name = model.Name,
        AcademicYear = model.AcademicYear,
        Department = model.Department
    };

    public static UpdateCollegeClassDto ToDto(this UpdateCollegeClassRequestModel model) => new()
    {
        Name = model.Name,
        AcademicYear = model.AcademicYear,
        Department = model.Department
    };

    public static CreateCollegeBatchDto ToDto(this CreateCollegeBatchRequestModel model) => new()
    {
        Name = model.Name,
        Description = model.Description,
        Capacity = model.Capacity,
        SubjectId = model.SubjectId,
        SubjectName = model.SubjectName
    };

    public static UpdateCollegeBatchDto ToDto(this UpdateCollegeBatchRequestModel model) => new()
    {
        Name = model.Name,
        Description = model.Description,
        Capacity = model.Capacity,
        SubjectId = model.SubjectId,
        SubjectName = model.SubjectName
    };

    public static AssignBatchTrainersDto ToDto(this AssignBatchTrainersRequestModel model) => new()
    {
        TrainerIds = model.TrainerIds ?? []
    };

    public static AssignStudentToBatchDto ToDto(this AssignStudentToBatchRequestModel model) => new()
    {
        StudentIds = model.StudentIds ?? []
    };
}
