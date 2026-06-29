using Taskverse.Api.Models;
using Taskverse.Business.DTOs;

namespace Taskverse.Api.Mappings;

public static class AttendanceMappings
{
    public static AttendanceBatchGroupResponseModel ToResponseModel(this AttendanceBatchGroupDto dto) => new()
    {
        ClassId = dto.ClassId,
        ClassName = dto.ClassName,
        AcademicYear = dto.AcademicYear,
        Batches = dto.Batches.Select(item => item.ToResponseModel()).ToList()
    };

    public static AttendanceBatchOptionResponseModel ToResponseModel(this AttendanceBatchOptionDto dto) => new()
    {
        BatchId = dto.BatchId,
        BatchName = dto.BatchName,
        BatchOwnerTrainerName = dto.BatchOwnerTrainerName
    };

    public static AttendanceStudentResponseModel ToResponseModel(this AttendanceStudentDto dto) => new()
    {
        StudentId = dto.StudentId,
        UserId = dto.UserId,
        FullName = dto.FullName,
        Email = dto.Email,
        EnrollmentNumber = dto.EnrollmentNumber,
        AttendanceEntry = dto.AttendanceEntry
    };

    public static AttendanceRosterResponseModel ToResponseModel(this AttendanceRosterDto dto) => new()
    {
        ClassId = dto.ClassId,
        ClassName = dto.ClassName,
        AcademicYear = dto.AcademicYear,
        BatchId = dto.BatchId,
        BatchName = dto.BatchName,
        AttendanceDate = dto.AttendanceDate,
        AttendanceSession = dto.AttendanceSession,
        IsSubmitted = dto.IsSubmitted,
        IsLocked = dto.IsLocked,
        CanEdit = dto.CanEdit,
        SubmittedByTrainerName = dto.SubmittedByTrainerName,
        BatchOwnerTrainerName = dto.BatchOwnerTrainerName,
        SubmittedAt = dto.SubmittedAt,
        LastModifiedAt = dto.LastModifiedAt,
        TotalStudents = dto.TotalStudents,
        PresentCount = dto.PresentCount,
        AbsentCount = dto.AbsentCount,
        AttendancePercentage = dto.AttendancePercentage,
        Students = dto.Students.Select(item => item.ToResponseModel()).ToList()
    };

    public static SubmitAttendanceDto ToDto(this SubmitAttendanceRequestModel model, Guid collegeId, Guid requesterUserId) => new()
    {
        CollegeId = collegeId,
        RequesterUserId = requesterUserId,
        BatchId = model.BatchId,
        AttendanceDate = model.AttendanceDate,
        AttendanceSession = model.AttendanceSession,
        Entries = model.Entries.Select(item => item.ToDto()).ToList()
    };

    public static SubmitAttendanceEntryDto ToDto(this SubmitAttendanceEntryRequestModel model) => new()
    {
        StudentId = model.StudentId,
        AttendanceEntry = model.AttendanceEntry
    };

    public static AttendanceHistoryResponseModel ToResponseModel(this AttendanceHistoryDto dto) => new()
    {
        BatchId = dto.BatchId,
        BatchName = dto.BatchName,
        FromDate = dto.FromDate,
        ToDate = dto.ToDate,
        Items = dto.Items.Select(item => item.ToResponseModel()).ToList()
    };

    public static AttendanceHistoryItemResponseModel ToResponseModel(this AttendanceHistoryItemDto dto) => new()
    {
        AttendanceSessionId = dto.AttendanceSessionId,
        AttendanceDate = dto.AttendanceDate,
        AttendanceSession = dto.AttendanceSession,
        SubmittedByTrainerName = dto.SubmittedByTrainerName,
        BatchOwnerTrainerName = dto.BatchOwnerTrainerName,
        SubmittedAt = dto.SubmittedAt,
        LastModifiedAt = dto.LastModifiedAt,
        IsLocked = dto.IsLocked,
        TotalStudents = dto.TotalStudents,
        PresentCount = dto.PresentCount,
        AbsentCount = dto.AbsentCount,
        AttendancePercentage = dto.AttendancePercentage
    };

    public static AttendanceEmailReportDto ToDto(this EmailAttendanceReportRequestModel model, Guid collegeId, Guid requesterUserId) => new()
    {
        CollegeId = collegeId,
        RequesterUserId = requesterUserId,
        BatchId = model.BatchId,
        FromDate = model.FromDate,
        ToDate = model.ToDate,
        RecipientEmails = model.RecipientEmails ?? []
    };
}
