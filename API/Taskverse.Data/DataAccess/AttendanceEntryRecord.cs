using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Taskverse.Data.Enums;

namespace Taskverse.Data.DataAccess;

[Table("attendance_entries")]
public class AttendanceEntryRecord
{
    [Key]
    [Column("attendance_entry_id")]
    public Guid AttendanceEntryId { get; set; } = Guid.NewGuid();

    [Column("attendance_session_id")]
    public Guid AttendanceSessionId { get; set; }

    [Column("student_id")]
    public Guid? StudentId { get; set; }

    [Column("attendance_entry")]
    public AttendanceEntryType AttendanceEntryType { get; set; }

    public AttendanceSession AttendanceSession { get; set; } = default!;
    public Student? Student { get; set; }
}
