using Taskverse.Api.MicroServices.Models;
using Taskverse.Business.DTOs;
using Taskverse.Business.Interface;

namespace Taskverse.Business.Mappings;

public static class ExamMappings
{
    public static ExamDto ToDto(this ExamModel model)
        => new()
        {
            ExamId = model.ExamId,
            Title = model.Title,
            Description = model.Description,
            DurationMinutes = model.DurationMinutes,
            TotalMarks = model.TotalMarks,
            PassingMarks = model.PassingMarks,
            IsActive = model.IsActive,
            CreatedBy = model.CreatedBy,
            CreatedAt = model.CreatedAt
        };

    public static QuestionDto ToDto(this QuestionModel model)
        => new()
        {
            QuestionId = model.QuestionId,
            ExamId = model.ExamId,
            Text = model.Text,
            Type = model.Type,
            Options = model.Options,
            Marks = model.Marks,
            Order = model.Order
        };

    public static ExamResultDto ToDto(this ExamResultModel model)
        => new()
        {
            SubmissionId = model.SubmissionId,
            ExamId = model.ExamId,
            UserId = model.UserId,
            Score = model.Score,
            TotalMarks = model.TotalMarks,
            IsPassed = model.IsPassed,
            CompletedAt = model.CompletedAt
        };

    public static CreateExamModel ToMicroServiceModel(this CreateExamDto dto)
        => new(dto.Title, dto.Description, dto.DurationMinutes, dto.TotalMarks, dto.PassingMarks, dto.CreatedBy);

    public static ExamSubmissionModel ToMicroServiceModel(this ExamSubmissionDto dto)
        => new(
            dto.ExamId,
            dto.UserId,
            dto.Answers.Select(a => new AnswerModel(a.QuestionId, a.Answer)).ToList(),
            dto.SubmittedAt);
}
