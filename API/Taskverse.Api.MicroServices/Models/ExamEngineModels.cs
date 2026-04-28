namespace Taskverse.Api.MicroServices.Models;

public record ExamModel(
    string ExamId,
    string Title,
    string? Description,
    int DurationMinutes,
    int TotalMarks,
    int PassingMarks,
    bool IsActive,
    string CreatedBy,
    DateTime CreatedAt);

public record QuestionModel(
    string QuestionId,
    string ExamId,
    string Text,
    string Type,
    List<string>? Options,
    string CorrectAnswer,
    int Marks,
    int Order);

public record CreateExamModel(
    string Title,
    string? Description,
    int DurationMinutes,
    int TotalMarks,
    int PassingMarks,
    string CreatedBy);

public record ExamSubmissionModel(
    string ExamId,
    string UserId,
    List<AnswerModel> Answers,
    DateTime SubmittedAt);

public record AnswerModel(string QuestionId, string Answer);

public record ExamResultModel(
    string SubmissionId,
    string ExamId,
    string UserId,
    int Score,
    int TotalMarks,
    bool IsPassed,
    DateTime CompletedAt);
