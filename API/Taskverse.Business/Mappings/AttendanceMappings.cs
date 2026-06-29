using Taskverse.Api.MicroServices.Models;
using Taskverse.Business.DTOs;
using Taskverse.Data.Enums;

namespace Taskverse.Business.Mappings;

public static class AttendanceMappings
{
    public static AttendanceRosterRequestModel ToMicroServiceModel(this AttendanceRosterRequestDto dto) => new(
        dto.BatchId,
        dto.AttendanceDate,
        (int)dto.AttendanceSession,
        dto.RequesterUserId,
        dto.CollegeId);

    public static SubmitAttendanceRequestModel ToMicroServiceModel(this SubmitAttendanceDto dto) => new(
        dto.BatchId,
        dto.AttendanceDate,
        (int)dto.AttendanceSession,
        dto.Entries.Select(item => item.ToMicroServiceModel()).ToList(),
        dto.RequesterUserId,
        dto.CollegeId);

    public static SubmitAttendanceEntryModel ToMicroServiceModel(this SubmitAttendanceEntryDto dto) => new(
        dto.StudentId,
        (int)dto.AttendanceEntry);

    public static AttendanceBatchGroupDto ToDto(this AttendanceBatchGroupModel model) => new()
    {
        ClassId = model.ClassId,
        ClassName = model.ClassName,
        AcademicYear = model.AcademicYear,
        Batches = model.Batches.Select(item => item.ToDto()).ToList()
    };

    public static AttendanceBatchOptionDto ToDto(this AttendanceBatchOptionModel model) => new()
    {
        BatchId = model.BatchId,
        BatchName = model.BatchName,
        BatchOwnerTrainerName = model.BatchOwnerTrainerName
    };

    public static AttendanceStudentDto ToDto(this AttendanceStudentModel model) => new()
    {
        StudentId = model.StudentId,
        UserId = model.UserId,
        FullName = model.FullName,
        Email = model.Email,
        EnrollmentNumber = model.EnrollmentNumber,
        AttendanceEntry = model.AttendanceEntry.HasValue
            ? (AttendanceEntryType)model.AttendanceEntry.Value
            : null
    };

    public static AttendanceRosterDto ToDto(this AttendanceRosterModel model) => new()
    {
        ClassId = model.ClassId,
        ClassName = model.ClassName,
        AcademicYear = model.AcademicYear,
        BatchId = model.BatchId,
        BatchName = model.BatchName,
        AttendanceDate = model.AttendanceDate,
        AttendanceSession = (AttendanceSessionType)model.AttendanceSession,
        IsSubmitted = model.IsSubmitted,
        IsLocked = model.IsLocked,
        CanEdit = model.CanEdit,
        SubmittedByTrainerName = model.SubmittedByTrainerName,
        BatchOwnerTrainerName = model.BatchOwnerTrainerName,
        SubmittedAt = model.SubmittedAt,
        LastModifiedAt = model.LastModifiedAt,
        TotalStudents = model.TotalStudents,
        PresentCount = model.PresentCount,
        AbsentCount = model.AbsentCount,
        AttendancePercentage = model.AttendancePercentage,
        Students = model.Students.Select(item => item.ToDto()).ToList()
    };

    public static AttendanceHistoryDto ToDto(this AttendanceHistoryModel model) => new()
    {
        BatchId = model.BatchId,
        BatchName = model.BatchName,
        FromDate = model.FromDate,
        ToDate = model.ToDate,
        Items = model.Items.Select(item => item.ToDto()).ToList()
    };

    public static AttendanceHistoryItemDto ToDto(this AttendanceHistoryItemModel model) => new()
    {
        AttendanceSessionId = model.AttendanceSessionId,
        AttendanceDate = model.AttendanceDate,
        AttendanceSession = (AttendanceSessionType)model.AttendanceSession,
        SubmittedByTrainerName = model.SubmittedByTrainerName,
        BatchOwnerTrainerName = model.BatchOwnerTrainerName,
        SubmittedAt = model.SubmittedAt,
        LastModifiedAt = model.LastModifiedAt,
        IsLocked = model.IsLocked,
        TotalStudents = model.TotalStudents,
        PresentCount = model.PresentCount,
        AbsentCount = model.AbsentCount,
        AttendancePercentage = model.AttendancePercentage
    };

    public static AttendanceExportDto ToDto(this AttendanceExportModel model) => new()
    {
        FileName = model.FileName,
        ContentType = model.ContentType,
        ContentBase64 = model.ContentBase64,
        BatchId = model.BatchId,
        BatchName = model.BatchName,
        FromDate = model.FromDate,
        ToDate = model.ToDate,
        Entries = model.Entries.Select(item => item.ToDto()).ToList()
    };

    public static AttendanceExportEntryDto ToDto(this AttendanceExportEntryModel model) => new()
    {
        AttendanceSessionId = model.AttendanceSessionId,
        AttendanceDate = model.AttendanceDate,
        AttendanceSession = (AttendanceSessionType)model.AttendanceSession,
        StudentName = model.StudentName,
        EnrollmentNumber = model.EnrollmentNumber,
        Email = model.Email,
        AttendanceEntry = (AttendanceEntryType)model.AttendanceEntry,
        SubmittedByTrainerName = model.SubmittedByTrainerName,
        BatchOwnerTrainerName = model.BatchOwnerTrainerName,
        SubmittedAt = model.SubmittedAt,
        LastModifiedAt = model.LastModifiedAt
    };
}
