namespace Taskverse.Api.MicroServices.Models;

public record AttendanceBatchGroupModel(
    string ClassId,
    string ClassName,
    string? AcademicYear,
    List<AttendanceBatchOptionModel> Batches);

public record AttendanceBatchOptionModel(
    string BatchId,
    string BatchName,
    string? BatchOwnerTrainerName);

public record AttendanceStudentModel(
    string StudentId,
    string UserId,
    string FullName,
    string Email,
    string? EnrollmentNumber,
    int? AttendanceEntry);

public record AttendanceRosterModel(
    string ClassId,
    string ClassName,
    string? AcademicYear,
    string BatchId,
    string BatchName,
    DateTime AttendanceDate,
    int AttendanceSession,
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
    List<AttendanceStudentModel> Students);

public record AttendanceRosterRequestModel(
    Guid BatchId,
    DateTime AttendanceDate,
    int AttendanceSession,
    Guid RequesterUserId,
    Guid CollegeId);

public record SubmitAttendanceRequestModel(
    Guid BatchId,
    DateTime AttendanceDate,
    int AttendanceSession,
    List<SubmitAttendanceEntryModel> Entries,
    Guid RequesterUserId,
    Guid CollegeId);

public record SubmitAttendanceEntryModel(
    Guid StudentId,
    int AttendanceEntry);

public record AttendanceHistoryModel(
    string BatchId,
    string BatchName,
    DateTime FromDate,
    DateTime ToDate,
    List<AttendanceHistoryItemModel> Items);

public record AttendanceHistoryItemModel(
    string AttendanceSessionId,
    DateTime AttendanceDate,
    int AttendanceSession,
    string SubmittedByTrainerName,
    string? BatchOwnerTrainerName,
    DateTime SubmittedAt,
    DateTime LastModifiedAt,
    bool IsLocked,
    int TotalStudents,
    int PresentCount,
    int AbsentCount,
    decimal AttendancePercentage);

public record AttendanceExportModel(
    string FileName,
    string ContentType,
    string ContentBase64,
    string BatchId,
    string BatchName,
    DateTime FromDate,
    DateTime ToDate,
    List<AttendanceExportEntryModel> Entries);

public record AttendanceExportEntryModel(
    string AttendanceSessionId,
    DateTime AttendanceDate,
    int AttendanceSession,
    string StudentName,
    string? EnrollmentNumber,
    string Email,
    int AttendanceEntry,
    string SubmittedByTrainerName,
    string? BatchOwnerTrainerName,
    DateTime SubmittedAt,
    DateTime LastModifiedAt);
