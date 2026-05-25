namespace Taskverse.Api.Models;

public class CreateQuestionBankAssessmentRequestModel
{
    public string AssessmentName { get; set; } = string.Empty;
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

public class QuestionBankAssessmentResponseModel
{
    public Guid AssessmentId { get; set; }
    public Guid CollegeId { get; set; }
    public Guid? SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public Guid? TopicId { get; set; }
    public string? TopicName { get; set; }
    public string AssessmentName { get; set; } = string.Empty;
    public string AssessmentType { get; set; } = string.Empty;
    public string AssessmentStatus { get; set; } = string.Empty;
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
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public List<Guid> QuestionIds { get; set; } = [];
}

public class CreateQuestionRequestModel
{
    public string Stream { get; set; } = string.Empty;
    public Guid? SubjectId { get; set; }
    public string? Subject { get; set; }
    public Guid? TopicId { get; set; }
    public string? Topic { get; set; }
    public string TopicTag { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public List<string>? Options { get; set; }
    public string Answer { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public decimal Marks { get; set; }
    public decimal NegativeMarks { get; set; }
    public int DifficultyLevel { get; set; }
}

public class DeleteQuestionsRequestModel
{
    public List<Guid> QuestionIds { get; set; } = [];
}

public class DeleteQuestionsResponseModel
{
    public List<Guid> DeletedQuestionIds { get; set; } = [];
}

public class QuestionBankSearchRequestModel
{
    public int? DifficultyLevel { get; set; }
    public Guid? SubjectId { get; set; }
    public Guid? TopicId { get; set; }
    public string? Subject { get; set; }
    public string? Topic { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PagedQuestionBankResponseModel
{
    public List<QuestionResponseModel> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class QuestionResponseModel
{
    public Guid QuestionId { get; set; }
    public Guid CollegeId { get; set; }
    public Guid? SubjectId { get; set; }
    public Guid? TopicId { get; set; }
    public string? Stream { get; set; }
    public string? Subject { get; set; }
    public string? Topic { get; set; }
    public string? TopicTag { get; set; }
    public string QuestionType { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public List<string>? Options { get; set; }
    public string? Answer { get; set; }
    public string? Explanation { get; set; }
    public decimal Marks { get; set; }
    public decimal NegativeMarks { get; set; }
    public int DifficultyLevel { get; set; }
    public int Version { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

public class AssessmentQuestionListRequestModel
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class AssessmentQuestionListItemResponseModel
{
    public Guid QuestionId { get; set; }
    public int DisplayOrder { get; set; }
    public string QuestionType { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public List<string>? Options { get; set; }
    public decimal Marks { get; set; }
    public decimal NegativeMarks { get; set; }
    public int DifficultyLevel { get; set; }
}

public class PagedAssessmentQuestionListResponseModel
{
    public List<AssessmentQuestionListItemResponseModel> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class StudentAssessmentListResponseModel
{
    public Guid AssessmentId { get; set; }
    public string AssessmentName { get; set; } = string.Empty;
    public string AssessmentStatus { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int TotalMarks { get; set; }
    public int DifficultyLevel { get; set; }
    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
}

public class StudentAssessmentDetailResponseModel
{
    public string AssessmentName { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int TotalMarks { get; set; }
    public int TotalQuestions { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Instructions { get; set; }
}
