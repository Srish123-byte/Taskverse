namespace Taskverse.Business.DTOs;

public class AssessmentDto
{
    public string AssessmentId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string Type { get; set; } = default!;
    public string? ExamId { get; set; }
    public List<string>? ChallengeIds { get; set; }
    public List<string> AssignedTo { get; set; } = [];
    public DateTime? DueDate { get; set; }
    public bool IsActive { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}

public class CreateAssessmentDto
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string Type { get; set; } = default!;
    public string? ExamId { get; set; }
    public List<string>? ChallengeIds { get; set; }
    public List<string> AssignedTo { get; set; } = [];
    public DateTime? DueDate { get; set; }
    public string CreatedBy { get; set; } = default!;
}

public class AssessmentResultDto
{
    public string ResultId { get; set; } = default!;
    public string AssessmentId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string Status { get; set; } = default!;
    public int? Score { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class CreateQuestionDto
{
    public Guid CollegeId { get; set; }
    public string CreatedBy { get; set; } = default!;
    public string Stream { get; set; } = default!;
    public string Subject { get; set; } = default!;
    public string Topic { get; set; } = default!;
    public string TopicTag { get; set; } = default!;
    public string QuestionType { get; set; } = default!;
    public string QuestionText { get; set; } = default!;
    public List<string>? Options { get; set; }
    public string Answer { get; set; } = default!;
    public string? Explanation { get; set; }
    public decimal Marks { get; set; }
    public decimal NegativeMarks { get; set; }
    public int DifficultyLevel { get; set; }
}

public class DeleteQuestionsDto
{
    public string CreatedBy { get; set; } = default!;
    public List<Guid> QuestionIds { get; set; } = [];
}

public class AssessmentQuestionDto
{
    public Guid QuestionId { get; set; }
    public Guid CollegeId { get; set; }
    public string? Stream { get; set; }
    public string? Subject { get; set; }
    public string? Topic { get; set; }
    public string? TopicTag { get; set; }
    public string QuestionType { get; set; } = default!;
    public string QuestionText { get; set; } = default!;
    public List<string>? Options { get; set; }
    public string? Answer { get; set; }
    public string? Explanation { get; set; }
    public decimal Marks { get; set; }
    public decimal NegativeMarks { get; set; }
    public int DifficultyLevel { get; set; }
    public int Version { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

public class AssessmentSummaryDto
{
    public string AssessmentId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public int TotalAssigned { get; set; }
    public int TotalCompleted { get; set; }
    public double? AverageScore { get; set; }
}
