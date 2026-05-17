using System.ComponentModel.DataAnnotations.Schema;
using Taskverse.Business.Enums;
namespace Taskverse.Data.DataAccess;

[Table("students")]
public class Student
{
    public Guid StudentId { get; set; }
    public Guid UserId { get; set; }
    public Guid CollegeId { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? BatchId { get; set; }

    public string? EnrollmentNumber { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }

    public int StatusId { get; set; }
    public UserStatus Status { get; set; }

    // Assessment / streak columns – nullable so approval works before any activity
    public int? CurrentStreak { get; set; }
    public int? LongestStreak { get; set; }
    public DateTime? LastAssessmentDate { get; set; }
    public int? TotalAssessmentsTaken { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public Guid? ApprovedBy { get; set; }

    // Navigation properties
    public User User { get; set; }
    public College College { get; set; }
    public Class? Class { get; set; }
    public Batch? Batch { get; set; }
}
