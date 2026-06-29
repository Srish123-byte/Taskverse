using Taskverse.Data.Enums;

namespace Taskverse.API.College.Service.Models;

public record AttendanceBatchGroupRecord(
    string ClassId,
    string ClassName,
    string? AcademicYear,
    List<AttendanceBatchOptionRecord> Batches);

public record AttendanceBatchOptionRecord(
    string BatchId,
    string BatchName,
    string? BatchOwnerTrainerName);

public record AttendanceStudentRecord(
    string StudentId,
    string UserId,
    string FullName,
    string Email,
    string? EnrollmentNumber,
    AttendanceEntryType? AttendanceEntry);

public record AttendanceRosterRecord(
    string ClassId,
    string ClassName,
    string? AcademicYear,
    string BatchId,
    string BatchName,
    DateTime AttendanceDate,
    AttendanceSessionType AttendanceSession,
    bool IsSubmitted,
    bool IsLocked,
    bool CanEdit,
    string? SubmittedByTrainerName,
    string? BatchOwnerTrainerName,
    DateTime? SubmittedAt,
    DateTime? LastModifiedAt,
    int TotalStudents,
    int PresentCount,
    int AbsentCount,
    decimal AttendancePercentage,
    List<AttendanceStudentRecord> Students);

public record AttendanceRosterRequest(
    Guid BatchId,
    DateTime AttendanceDate,
    AttendanceSessionType AttendanceSession,
    Guid RequesterUserId,
    Guid CollegeId);

public record SubmitAttendanceRequest(
    Guid BatchId,
    DateTime AttendanceDate,
    AttendanceSessionType AttendanceSession,
    List<SubmitAttendanceEntryRecord> Entries,
    Guid RequesterUserId,
    Guid CollegeId);

public record SubmitAttendanceEntryRecord(
    Guid StudentId,
    AttendanceEntryType AttendanceEntry);

public record AttendanceHistoryRecord(
    string BatchId,
    string BatchName,
    DateTime FromDate,
    DateTime ToDate,
    List<AttendanceHistoryItemRecord> Items);

public record AttendanceHistoryItemRecord(
    string AttendanceSessionId,
    DateTime AttendanceDate,
    AttendanceSessionType AttendanceSession,
    string SubmittedByTrainerName,
    string? BatchOwnerTrainerName,
    DateTime SubmittedAt,
    DateTime LastModifiedAt,
    bool IsLocked,
    int TotalStudents,
    int PresentCount,
    int AbsentCount,
    decimal AttendancePercentage);

public record AttendanceExportRecord(
    string FileName,
    string ContentType,
    string ContentBase64,
    string BatchId,
    string BatchName,
    DateTime FromDate,
    DateTime ToDate,
    List<AttendanceExportEntryRecord> Entries);

public record AttendanceExportEntryRecord(
    string AttendanceSessionId,
    DateTime AttendanceDate,
    AttendanceSessionType AttendanceSession,
    string StudentName,
    string? EnrollmentNumber,
    string Email,
    AttendanceEntryType AttendanceEntry,
    string SubmittedByTrainerName,
    string? BatchOwnerTrainerName,
    DateTime SubmittedAt,
    DateTime LastModifiedAt);

public record AttendanceExportRequest(
    Guid BatchId,
    DateTime FromDate,
    DateTime ToDate,
    Guid RequesterUserId,
    Guid CollegeId);
