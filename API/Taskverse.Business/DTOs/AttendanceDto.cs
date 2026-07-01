using Taskverse.Data.Enums;

namespace Taskverse.Business.DTOs;

public class AttendanceBatchGroupDto
{
    public string ClassId { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
    public List<AttendanceBatchOptionDto> Batches { get; set; } = [];
}

public class AttendanceBatchOptionDto
{
    public string BatchId { get; set; } = string.Empty;
    public string BatchName { get; set; } = string.Empty;
    public string? BatchOwnerTrainerName { get; set; }
}

public class AttendanceStudentDto
{
    public string StudentId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? EnrollmentNumber { get; set; }
    public AttendanceEntryType? AttendanceEntry { get; set; }
}

public class AttendanceRosterDto
{
    public string ClassId { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
    public string BatchId { get; set; } = string.Empty;
    public string BatchName { get; set; } = string.Empty;
    public DateTime AttendanceDate { get; set; }
    public AttendanceSessionType AttendanceSession { get; set; }
    public bool IsSubmitted { get; set; }
    public bool IsLocked { get; set; }
    public bool CanEdit { get; set; }
    public string? SubmittedByTrainerName { get; set; }
    public string? BatchOwnerTrainerName { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public int TotalStudents { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public decimal AttendancePercentage { get; set; }
    public List<AttendanceStudentDto> Students { get; set; } = [];
}

public class AttendanceRosterRequestDto
{
    public Guid CollegeId { get; set; }
    public Guid RequesterUserId { get; set; }
    public Guid BatchId { get; set; }
    public DateTime AttendanceDate { get; set; }
    public AttendanceSessionType AttendanceSession { get; set; }
}

public class SubmitAttendanceDto
{
    public Guid CollegeId { get; set; }
    public Guid RequesterUserId { get; set; }
    public Guid BatchId { get; set; }
    public DateTime AttendanceDate { get; set; }
    public AttendanceSessionType AttendanceSession { get; set; }
    public List<SubmitAttendanceEntryDto> Entries { get; set; } = [];
}

public class SubmitAttendanceEntryDto
{
    public Guid StudentId { get; set; }
    public AttendanceEntryType AttendanceEntry { get; set; }
}

public class AttendanceHistoryRequestDto
{
    public Guid CollegeId { get; set; }
    public Guid RequesterUserId { get; set; }
    public Guid BatchId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
}

public class AttendanceHistoryDto
{
    public string BatchId { get; set; } = string.Empty;
    public string BatchName { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<AttendanceHistoryItemDto> Items { get; set; } = [];
}

public class AttendanceHistoryItemDto
{
    public string AttendanceSessionId { get; set; } = string.Empty;
    public DateTime AttendanceDate { get; set; }
    public AttendanceSessionType AttendanceSession { get; set; }
    public string SubmittedByTrainerName { get; set; } = string.Empty;
    public string? BatchOwnerTrainerName { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public bool IsLocked { get; set; }
    public int TotalStudents { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public decimal AttendancePercentage { get; set; }
}

public class AttendanceExportDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string ContentBase64 { get; set; } = string.Empty;
    public string BatchId { get; set; } = string.Empty;
    public string BatchName { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<AttendanceExportEntryDto> Entries { get; set; } = [];
}

public class AttendanceExportEntryDto
{
    public string AttendanceSessionId { get; set; } = string.Empty;
    public DateTime AttendanceDate { get; set; }
    public AttendanceSessionType AttendanceSession { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string? EnrollmentNumber { get; set; }
    public string Email { get; set; } = string.Empty;
    public AttendanceEntryType AttendanceEntry { get; set; }
    public string SubmittedByTrainerName { get; set; } = string.Empty;
    public string? BatchOwnerTrainerName { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
}

public class AttendanceEmailReportDto
{
    public Guid CollegeId { get; set; }
    public Guid RequesterUserId { get; set; }
    public Guid BatchId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<string> RecipientEmails { get; set; } = [];
}
