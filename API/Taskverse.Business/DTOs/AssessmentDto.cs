namespace Taskverse.Business.DTOs;

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

public class DeleteAssessmentDto
{
    public Guid AssessmentId { get; set; }
    public string DeletedBy { get; set; } = default!;
    public string RequesterRole { get; set; } = default!;
    public Guid? CollegeId { get; set; }
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

public class StudentAssessmentListItemDto
{
    public Guid AssessmentId { get; set; }
    public string AssessmentName { get; set; } = default!;
    public string AssessmentStatus { get; set; } = default!;
    public int DurationMinutes { get; set; }
    public int TotalMarks { get; set; }
    public int DifficultyLevel { get; set; }
    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
}

public class StudentAssessmentDetailDto
{
    public string AssessmentName { get; set; } = default!;
    public int DurationMinutes { get; set; }
    public int TotalMarks { get; set; }
    public int TotalQuestions { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Instructions { get; set; }
}

public class StudentAssessmentStartDto
{
    public Guid AttemptId { get; set; }
    public Guid AssessmentId { get; set; }
    public string AttemptStatus { get; set; } = default!;
    public DateTime? StartedAt { get; set; }
}

public class SaveStudentAttemptAnswerDto
{
    public string? SelectedAnswer { get; set; }
}

public class StudentAttemptAnswerDto
{
    public Guid QuestionId { get; set; }
    public string? SelectedAnswer { get; set; }
    public DateTime? AnsweredAt { get; set; }
}

public class StudentAttemptSubmitDto
{
    public Guid AttemptId { get; set; }
    public string AttemptStatus { get; set; } = default!;
    public DateTime? SubmittedAt { get; set; }
}

public class StudentAttemptRecoveryQuestionDto
{
    public Guid QuestionId { get; set; }
    public int DisplayOrder { get; set; }
    public string QuestionType { get; set; } = default!;
    public string QuestionText { get; set; } = default!;
    public List<string>? Options { get; set; }
    public decimal Marks { get; set; }
    public decimal NegativeMarks { get; set; }
    public int DifficultyLevel { get; set; }
    public string? SelectedAnswer { get; set; }
    public DateTime? AnsweredAt { get; set; }
}

public class StudentAttemptRecoveryDto
{
    public Guid AttemptId { get; set; }
    public Guid AssessmentId { get; set; }
    public string AssessmentName { get; set; } = default!;
    public string AttemptStatus { get; set; } = default!;
    public DateTime? StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int RemainingSeconds { get; set; }
    public int DurationMinutes { get; set; }
    public int TotalMarks { get; set; }
    public int TotalQuestions { get; set; }
    public int AttemptedQuestions { get; set; }
    public int UnansweredQuestions { get; set; }
    public string? Instructions { get; set; }
    public List<StudentAttemptRecoveryQuestionDto> Questions { get; set; } = [];
}
