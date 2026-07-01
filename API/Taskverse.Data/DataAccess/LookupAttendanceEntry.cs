using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("lookup_attendance_entry")]
public class LookupAttendanceEntry
{
    [Key]
    [Column("attendance_entry_id")]
    public int AttendanceEntryId { get; set; }

    [Column("attendance_entry")]
    public string AttendanceEntry { get; set; } = string.Empty;
}
