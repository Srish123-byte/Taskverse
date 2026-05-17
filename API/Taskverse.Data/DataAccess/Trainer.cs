using System.ComponentModel.DataAnnotations.Schema;
using Taskverse.Business.Enums;
namespace Taskverse.Data.DataAccess;

[Table("trainers")]
public class Trainer
{
    public Guid TrainerId { get; set; }
    public Guid UserId { get; set; }
    public Guid CollegeId { get; set; }

    public string FullName { get; set; }
    public string Email { get; set; }
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }

    public int StatusId { get; set; }
    public UserStatus Status { get; set; }

    // Assessment count columns – nullable so approval works before any assessments are assigned
    public int? UpcomingAssessmentsCount { get; set; }
    public int? LiveAssessmentsCount { get; set; }
    public int? CompletedAssessmentsCount { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public Guid? ApprovedBy { get; set; }

    // Navigation properties
    public User User { get; set; }
    public College College { get; set; }
    public ICollection<TrainerClass> TrainerClasses { get; set; }
    public ICollection<TrainerBatch> TrainerBatches { get; set; }
}
