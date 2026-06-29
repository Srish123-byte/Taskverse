using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("lookup_attendance_session")]
public class LookupAttendanceSession
{
    [Key]
    [Column("attendance_session_id")]
    public int AttendanceSessionId { get; set; }

    [Column("attendance_session")]
    public string AttendanceSession { get; set; } = string.Empty;
}
