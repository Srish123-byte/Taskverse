using log4net;
using Taskverse.Api.MicroServices.Interfaces;
using Taskverse.Api.MicroServices.Models;
using Taskverse.Business.DTOs;
using Taskverse.Business.Interface;
using Taskverse.Business.Mappings;
using Taskverse.Business.Utilities;

namespace Taskverse.Business.Orchestrators;

public class ExamOrchestrator : IExamOrchestrator
{
    private readonly IMicroServiceOrchestrator _microServiceOrchestrator;
    private static readonly ILog _log = LogManager.GetLogger(typeof(ExamOrchestrator));

    public ExamOrchestrator(IMicroServiceOrchestrator microServiceOrchestrator)
    {
        _microServiceOrchestrator = microServiceOrchestrator;
    }

    public async Task<ExamDto> GetExam(string examId)
    {
        _log.Debug($"ExamOrchestrator.GetExam: examId={examId}");

        var result = await _microServiceOrchestrator.GetExam(examId);
        result.EnsureSuccess(nameof(GetExam));

        ExamModel model = result.DeserializeValue<ExamModel>()
            ?? throw new InvalidOperationException($"GetExam returned an empty response for examId={examId}.");

        return model.ToDto();
    }

    public async Task<ExamDto> CreateExam(CreateExamDto dto)
    {
        _log.Debug($"ExamOrchestrator.CreateExam: title={dto.Title}");

        var result = await _microServiceOrchestrator.CreateExam(dto.ToMicroServiceModel());
        result.EnsureSuccess(nameof(CreateExam));

        ExamModel model = result.DeserializeValue<ExamModel>()
            ?? throw new InvalidOperationException("CreateExam returned an empty response.");

        return model.ToDto();
    }

    public async Task<List<QuestionDto>> GetExamQuestions(string examId)
    {
        _log.Debug($"ExamOrchestrator.GetExamQuestions: examId={examId}");

        var result = await _microServiceOrchestrator.GetExamQuestions(examId);
        result.EnsureSuccess(nameof(GetExamQuestions));

        List<QuestionModel> models = result.DeserializeValue<List<QuestionModel>>()
            ?? throw new InvalidOperationException($"GetExamQuestions returned an empty response for examId={examId}.");

        return models.Select(q => q.ToDto()).ToList();
    }

    public async Task<ExamResultDto> SubmitExam(ExamSubmissionDto dto)
    {
        _log.Debug($"ExamOrchestrator.SubmitExam: examId={dto.ExamId}, userId={dto.UserId}");

        var result = await _microServiceOrchestrator.SubmitExam(dto.ToMicroServiceModel());
        result.EnsureSuccess(nameof(SubmitExam));

        ExamResultModel model = result.DeserializeValue<ExamResultModel>()
            ?? throw new InvalidOperationException("SubmitExam returned an empty response.");

        return model.ToDto();
    }

    public async Task<ExamResultDto> GetExamResult(string submissionId)
    {
        _log.Debug($"ExamOrchestrator.GetExamResult: submissionId={submissionId}");

        var result = await _microServiceOrchestrator.GetExamResult(submissionId);
        result.EnsureSuccess(nameof(GetExamResult));

        ExamResultModel model = result.DeserializeValue<ExamResultModel>()
            ?? throw new InvalidOperationException($"GetExamResult returned an empty response for submissionId={submissionId}.");

        return model.ToDto();
    }

    public async Task<List<ExamDto>> GetExamsByUser(string userId)
    {
        _log.Debug($"ExamOrchestrator.GetExamsByUser: userId={userId}");

        var result = await _microServiceOrchestrator.GetExamsByUser(userId);
        result.EnsureSuccess(nameof(GetExamsByUser));

        List<ExamModel> models = result.DeserializeValue<List<ExamModel>>()
            ?? throw new InvalidOperationException($"GetExamsByUser returned an empty response for userId={userId}.");

        return models.Select(e => e.ToDto()).ToList();
    }
}
