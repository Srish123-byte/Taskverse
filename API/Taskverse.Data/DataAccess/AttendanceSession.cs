using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Taskverse.Data.Enums;

namespace Taskverse.Data.DataAccess;

[Table("attendance_sessions")]
public class AttendanceSession
{
    [Key]
    [Column("attendance_session_id")]
    public Guid AttendanceSessionId { get; set; } = Guid.NewGuid();

    [Column("college_id")]
    public Guid CollegeId { get; set; }

    [Column("class_id")]
    public Guid? ClassId { get; set; }

    [Column("batch_id")]
    public Guid? BatchId { get; set; }

    [Column("attendance_date")]
    public DateTime AttendanceDate { get; set; }

    [Column("attendance_session")]
    public AttendanceSessionType AttendanceSessionType { get; set; }

    [Column("submitted_by_trainer_id")]
    public Guid? SubmittedByTrainerId { get; set; }

    [Column("batch_owner_trainer_id")]
    public Guid? BatchOwnerTrainerId { get; set; }

    [Column("submitted_at")]
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    [Column("modified_at")]
    public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;

    public College College { get; set; } = default!;
    public Class? Class { get; set; }
    public Batch? Batch { get; set; }
    public Trainer? SubmittedByTrainer { get; set; }
    public Trainer? BatchOwnerTrainer { get; set; }
    public ICollection<AttendanceEntryRecord> Entries { get; set; } = [];
}
