namespace Taskverse.Business.DTOs;

public class ExamDto
{
    public string ExamId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public int TotalMarks { get; set; }
    public int PassingMarks { get; set; }
    public bool IsActive { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}

public class CreateExamDto
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public int TotalMarks { get; set; }
    public int PassingMarks { get; set; }
    public string CreatedBy { get; set; } = default!;
}

public class QuestionDto
{
    public string QuestionId { get; set; } = default!;
    public string ExamId { get; set; } = default!;
    public string Text { get; set; } = default!;
    public string Type { get; set; } = default!;
    public List<string>? Options { get; set; }
    public int Marks { get; set; }
    public int Order { get; set; }
}

public class ExamSubmissionDto
{
    public string ExamId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public List<AnswerDto> Answers { get; set; } = [];
    public DateTime SubmittedAt { get; set; }
}

public class AnswerDto
{
    public string QuestionId { get; set; } = default!;
    public string Answer { get; set; } = default!;
}

public class ExamResultDto
{
    public string SubmissionId { get; set; } = default!;
    public string ExamId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public int Score { get; set; }
    public int TotalMarks { get; set; }
    public bool IsPassed { get; set; }
    public DateTime CompletedAt { get; set; }
}
