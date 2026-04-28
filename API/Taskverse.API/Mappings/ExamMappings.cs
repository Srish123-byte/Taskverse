using Taskverse.Api.Models;
using Taskverse.Business.DTOs;
using Taskverse.Business.Interface;

namespace Taskverse.Api.Mappings;

public static class ExamMappings
{
    public static CreateExamDto ToDto(this CreateExamRequestModel model) => new()
    {
        Title = model.Title,
        Description = model.Description,
        DurationMinutes = model.DurationMinutes,
        TotalMarks = model.TotalMarks,
        PassingMarks = model.PassingMarks,
        CreatedBy = model.CreatedBy
    };

    public static ExamSubmissionDto ToDto(this ExamSubmissionRequestModel model) => new()
    {
        ExamId = model.ExamId,
        UserId = model.UserId,
        Answers = model.Answers.Select(a => new AnswerDto { QuestionId = a.QuestionId, Answer = a.Answer }).ToList(),
        SubmittedAt = model.SubmittedAt
    };

    public static ExamResponseModel ToResponseModel(this ExamDto dto) => new()
    {
        ExamId = dto.ExamId,
        Title = dto.Title,
        Description = dto.Description,
        DurationMinutes = dto.DurationMinutes,
        TotalMarks = dto.TotalMarks,
        PassingMarks = dto.PassingMarks,
        IsActive = dto.IsActive,
        CreatedBy = dto.CreatedBy,
        CreatedAt = dto.CreatedAt
    };

    public static QuestionResponseModel ToResponseModel(this QuestionDto dto) => new()
    {
        QuestionId = dto.QuestionId,
        ExamId = dto.ExamId,
        Text = dto.Text,
        Type = dto.Type,
        Options = dto.Options,
        Marks = dto.Marks,
        Order = dto.Order
    };

    public static ExamResultResponseModel ToResponseModel(this ExamResultDto dto) => new()
    {
        SubmissionId = dto.SubmissionId,
        ExamId = dto.ExamId,
        UserId = dto.UserId,
        Score = dto.Score,
        TotalMarks = dto.TotalMarks,
        IsPassed = dto.IsPassed,
        CompletedAt = dto.CompletedAt
    };
}
