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

public class CreateQuestionBankAssessmentDto
{
    public Guid CollegeId { get; set; }
    public string CreatedBy { get; set; } = default!;
    public string AssessmentName { get; set; } = default!;
    public Guid? SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public Guid? TopicId { get; set; }
    public string? TopicName { get; set; }
    public Guid[] AssignedBatchIds { get; set; } = [];
    public List<Guid> QuestionIds { get; set; } = [];
    public int DurationMinutes { get; set; }
    public int TotalMarks { get; set; }
    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
}

public class QuestionBankAssessmentDto
{
    public Guid AssessmentId { get; set; }
    public Guid CollegeId { get; set; }
    public Guid? SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public Guid? TopicId { get; set; }
    public string? TopicName { get; set; }
    public string AssessmentName { get; set; } = default!;
    public string AssessmentType { get; set; } = default!;
    public string AssessmentStatus { get; set; } = default!;
    public int DurationMinutes { get; set; }
    public int TotalMarks { get; set; }
    public int DifficultyLevel { get; set; }
    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
    public string? Instructions { get; set; }
    public Guid[] AssignedBatchIds { get; set; } = [];
    public bool AllowLateEntry { get; set; }
    public bool ShowResultsImmediately { get; set; }
    public bool AllowQuestionReview { get; set; }
    public bool NegativeMarking { get; set; }
    public decimal MarksPerQuestion { get; set; }
    public bool IsTotalMarksAutoCalculated { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public List<Guid> QuestionIds { get; set; } = [];
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
    public Guid? SubjectId { get; set; }
    public string? Subject { get; set; }
    public Guid? TopicId { get; set; }
    public string? Topic { get; set; }
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

public class QuestionBankSearchDto
{
    public Guid CollegeId { get; set; }
    public int? DifficultyLevel { get; set; }
    public Guid? SubjectId { get; set; }
    public Guid? TopicId { get; set; }
    public string? Subject { get; set; }
    public string? Topic { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PagedQuestionBankDto
{
    public List<AssessmentQuestionDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class AssessmentQuestionDto
{
    public Guid QuestionId { get; set; }
    public Guid CollegeId { get; set; }
    public Guid? SubjectId { get; set; }
    public Guid? TopicId { get; set; }
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

public class AssessmentQuestionListItemDto
{
    public Guid QuestionId { get; set; }
    public int DisplayOrder { get; set; }
    public string QuestionType { get; set; } = default!;
    public string QuestionText { get; set; } = default!;
    public List<string>? Options { get; set; }
    public decimal Marks { get; set; }
    public decimal NegativeMarks { get; set; }
    public int DifficultyLevel { get; set; }
}

public class PagedAssessmentQuestionListDto
{
    public List<AssessmentQuestionListItemDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}
