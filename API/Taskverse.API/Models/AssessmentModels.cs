namespace Taskverse.Api.Models;

public class CreateQuestionRequestModel
{
    public string Stream { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
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

public class QuestionResponseModel
{
    public Guid QuestionId { get; set; }
    public Guid CollegeId { get; set; }
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
