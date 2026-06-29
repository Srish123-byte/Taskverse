using Taskverse.Data.Enums;

namespace Taskverse.Api.Models;

public class AttendanceBatchGroupResponseModel
{
    public string ClassId { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
    public List<AttendanceBatchOptionResponseModel> Batches { get; set; } = [];
}

public class AttendanceBatchOptionResponseModel
{
    public string BatchId { get; set; } = string.Empty;
    public string BatchName { get; set; } = string.Empty;
    public string? BatchOwnerTrainerName { get; set; }
}

public class AttendanceStudentResponseModel
{
    public string StudentId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? EnrollmentNumber { get; set; }
    public AttendanceEntryType? AttendanceEntry { get; set; }
}

public class AttendanceRosterResponseModel
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
    public List<AttendanceStudentResponseModel> Students { get; set; } = [];
}

public class SubmitAttendanceRequestModel
{
    public Guid BatchId { get; set; }
    public DateTime AttendanceDate { get; set; }
    public AttendanceSessionType AttendanceSession { get; set; }
    public List<SubmitAttendanceEntryRequestModel> Entries { get; set; } = [];
}

public class SubmitAttendanceEntryRequestModel
{
    public Guid StudentId { get; set; }
    public AttendanceEntryType AttendanceEntry { get; set; }
}

public class AttendanceHistoryResponseModel
{
    public string BatchId { get; set; } = string.Empty;
    public string BatchName { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<AttendanceHistoryItemResponseModel> Items { get; set; } = [];
}

public class AttendanceHistoryItemResponseModel
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

public class EmailAttendanceReportRequestModel
{
    public Guid BatchId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<string> RecipientEmails { get; set; } = [];
}
