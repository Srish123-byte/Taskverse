using Taskverse.API.College.Service.DTOs;
using Taskverse.API.College.Service.Models;

namespace Taskverse.API.College.Service.Mappings;

public static class AttendanceMappings
{
    public static AttendanceRosterRequestDto ToDto(this AttendanceRosterRequest model) => new()
    {
        BatchId = model.BatchId,
        AttendanceDate = model.AttendanceDate,
        AttendanceSession = model.AttendanceSession,
        RequesterUserId = model.RequesterUserId,
        CollegeId = model.CollegeId
    };

    public static SubmitAttendanceDto ToDto(this SubmitAttendanceRequest model) => new()
    {
        BatchId = model.BatchId,
        AttendanceDate = model.AttendanceDate,
        AttendanceSession = model.AttendanceSession,
        Entries = model.Entries.Select(item => item.ToDto()).ToList(),
        RequesterUserId = model.RequesterUserId,
        CollegeId = model.CollegeId
    };

    public static SubmitAttendanceEntryDto ToDto(this SubmitAttendanceEntryRecord model) => new()
    {
        StudentId = model.StudentId,
        AttendanceEntry = model.AttendanceEntry
    };

    public static AttendanceBatchGroupRecord ToModel(this AttendanceBatchGroupDto dto) => new(
        dto.ClassId,
        dto.ClassName,
        dto.AcademicYear,
        dto.Batches.Select(item => item.ToModel()).ToList());

    public static AttendanceBatchOptionRecord ToModel(this AttendanceBatchOptionDto dto) => new(
        dto.BatchId,
        dto.BatchName,
        dto.BatchOwnerTrainerName);

    public static AttendanceStudentRecord ToModel(this AttendanceStudentDto dto) => new(
        dto.StudentId,
        dto.UserId,
        dto.FullName,
        dto.Email,
        dto.EnrollmentNumber,
        dto.AttendanceEntry);

    public static AttendanceRosterRecord ToModel(this AttendanceRosterDto dto) => new(
        dto.ClassId,
        dto.ClassName,
        dto.AcademicYear,
        dto.BatchId,
        dto.BatchName,
        dto.AttendanceDate,
        dto.AttendanceSession,
        dto.IsSubmitted,
        dto.IsLocked,
        dto.CanEdit,
        dto.SubmittedByTrainerName,
        dto.BatchOwnerTrainerName,
        dto.SubmittedAt,
        dto.LastModifiedAt,
        dto.TotalStudents,
        dto.PresentCount,
        dto.AbsentCount,
        dto.AttendancePercentage,
        dto.Students.Select(item => item.ToModel()).ToList());

    public static AttendanceHistoryRecord ToModel(this AttendanceHistoryDto dto) => new(
        dto.BatchId,
        dto.BatchName,
        dto.FromDate,
        dto.ToDate,
        dto.Items.Select(item => item.ToModel()).ToList());

    public static AttendanceHistoryItemRecord ToModel(this AttendanceHistoryItemDto dto) => new(
        dto.AttendanceSessionId,
        dto.AttendanceDate,
        dto.AttendanceSession,
        dto.SubmittedByTrainerName,
        dto.BatchOwnerTrainerName,
        dto.SubmittedAt,
        dto.LastModifiedAt,
        dto.IsLocked,
        dto.TotalStudents,
        dto.PresentCount,
        dto.AbsentCount,
        dto.AttendancePercentage);

    public static AttendanceExportRecord ToModel(this AttendanceExportDto dto) => new(
        dto.FileName,
        dto.ContentType,
        dto.ContentBase64,
        dto.BatchId,
        dto.BatchName,
        dto.FromDate,
        dto.ToDate,
        dto.Entries.Select(item => item.ToModel()).ToList());

    public static AttendanceExportEntryRecord ToModel(this AttendanceExportEntryDto dto) => new(
        dto.AttendanceSessionId,
        dto.AttendanceDate,
        dto.AttendanceSession,
        dto.StudentName,
        dto.EnrollmentNumber,
        dto.Email,
        dto.AttendanceEntry,
        dto.SubmittedByTrainerName,
        dto.BatchOwnerTrainerName,
        dto.SubmittedAt,
        dto.LastModifiedAt);
}
