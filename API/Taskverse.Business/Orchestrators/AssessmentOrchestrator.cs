using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Taskverse.Api.MicroServices.Interfaces;
using Taskverse.Api.MicroServices.Models;
using Taskverse.Business.DTOs;
using Taskverse.Business.Interface;
using Taskverse.Business.Mappings;
using Taskverse.Business.Utilities;

namespace Taskverse.Business.Orchestrators;

public class AssessmentOrchestrator : IAssessmentOrchestrator
{
    private readonly IMicroServiceOrchestrator _microServiceOrchestrator;
    private readonly IAssessmentManager _assessmentManager;
    private static readonly ILog _log = LogManager.GetLogger(typeof(AssessmentOrchestrator));

    public AssessmentOrchestrator(
        IMicroServiceOrchestrator microServiceOrchestrator,
        IAssessmentManager assessmentManager)
    {
        _microServiceOrchestrator = microServiceOrchestrator;
        _assessmentManager = assessmentManager;
    }

    public async Task<AssessmentDto> GetAssessment(string assessmentId)
    {
        _log.Debug($"AssessmentOrchestrator.GetAssessment: assessmentId={assessmentId}");

        var result = await _microServiceOrchestrator.GetAssessment(assessmentId);
        result.EnsureSuccess(nameof(GetAssessment));

        AssessmentModel model = result.DeserializeValue<AssessmentModel>()
            ?? throw new InvalidOperationException($"GetAssessment returned an empty response for assessmentId={assessmentId}.");

        return model.ToDto();
    }

    public async Task<QuestionBankAssessmentDto> CreateAssessment(CreateQuestionBankAssessmentDto dto)
    {
        _log.Debug($"AssessmentOrchestrator.CreateAssessment: assessmentName={dto.AssessmentName}, collegeId={dto.CollegeId}, questionCount={dto.QuestionIds.Count}");

        var result = await _microServiceOrchestrator.CreateAssessment(dto.ToMicroServiceModel());

        if (result.IsSuccess())
        {
            var model = result.DeserializeValue<QuestionBankAssessmentModel>()
                ?? throw new InvalidOperationException("CreateAssessment returned an empty response.");

            return model.ToDto();
        }

        var message = ExtractMessage(result.Value) ?? $"CreateAssessment failed with status {result.StatusCode}.";

        throw result.StatusCode switch
        {
            StatusCodes.Status400BadRequest => new ArgumentException(message),
            StatusCodes.Status403Forbidden => new UnauthorizedAccessException(message),
            StatusCodes.Status404NotFound => new KeyNotFoundException(message),
            StatusCodes.Status409Conflict => new InvalidOperationException(message),
            StatusCodes.Status422UnprocessableEntity => new InvalidDataException(message),
            StatusCodes.Status503ServiceUnavailable => new HttpRequestException(message),
            _ => new Exception(message)
        };
    }

    public async Task<QuestionBankAssessmentDto> PublishAssessment(Guid assessmentId)
    {
        _log.Debug($"AssessmentOrchestrator.PublishAssessment: assessmentId={assessmentId}");

        var result = await _microServiceOrchestrator.PublishAssessment(assessmentId);

        if (result.IsSuccess())
        {
            var model = result.DeserializeValue<QuestionBankAssessmentModel>()
                ?? throw new InvalidOperationException("PublishAssessment returned an empty response.");

            return model.ToDto();
        }

        var message = ExtractMessage(result.Value) ?? $"PublishAssessment failed with status {result.StatusCode}.";

        throw result.StatusCode switch
        {
            StatusCodes.Status400BadRequest => new ArgumentException(message),
            StatusCodes.Status403Forbidden => new UnauthorizedAccessException(message),
            StatusCodes.Status404NotFound => new KeyNotFoundException(message),
            StatusCodes.Status409Conflict => new InvalidOperationException(message),
            StatusCodes.Status422UnprocessableEntity => new InvalidDataException(message),
            StatusCodes.Status503ServiceUnavailable => new HttpRequestException(message),
            _ => new Exception(message)
        };
    }

    public async Task<List<AssessmentQuestionDto>> CreateQuestions(List<CreateQuestionDto> dtos)
    {
        _log.Debug($"AssessmentOrchestrator.CreateQuestions: count={dtos.Count}");

        var result = await _microServiceOrchestrator.CreateQuestions(dtos.ToMicroServiceModels());

        if (result.IsSuccess())
        {
            var models = result.DeserializeValue<List<AssessmentQuestionModel>>()
                ?? throw new InvalidOperationException("CreateQuestions returned an empty response.");

            return models.Select(model => model.ToDto()).ToList();
        }

        var message = ExtractMessage(result.Value) ?? $"CreateQuestions failed with status {result.StatusCode}.";

        throw result.StatusCode switch
        {
            StatusCodes.Status400BadRequest => new ArgumentException(message),
            StatusCodes.Status403Forbidden => new UnauthorizedAccessException(message),
            StatusCodes.Status404NotFound => new KeyNotFoundException(message),
            StatusCodes.Status409Conflict => new InvalidOperationException(message),
            StatusCodes.Status503ServiceUnavailable => new HttpRequestException(message),
            _ => new Exception(message)
        };
    }

    public async Task<AssessmentQuestionDto> UpdateQuestion(Guid questionId, CreateQuestionDto dto)
    {
        _log.Debug($"AssessmentOrchestrator.UpdateQuestion: questionId={questionId}, collegeId={dto.CollegeId}, subject={dto.Subject}, topic={dto.Topic}");

        var result = await _microServiceOrchestrator.UpdateQuestion(questionId, dto.ToMicroServiceModel());

        if (result.IsSuccess())
        {
            var model = result.DeserializeValue<AssessmentQuestionModel>()
                ?? throw new InvalidOperationException("UpdateQuestion returned an empty response.");

            return model.ToDto();
        }

        var message = ExtractMessage(result.Value) ?? $"UpdateQuestion failed with status {result.StatusCode}.";

        throw result.StatusCode switch
        {
            StatusCodes.Status400BadRequest => new ArgumentException(message),
            StatusCodes.Status403Forbidden => new UnauthorizedAccessException(message),
            StatusCodes.Status404NotFound => new KeyNotFoundException(message),
            StatusCodes.Status409Conflict => new InvalidOperationException(message),
            StatusCodes.Status503ServiceUnavailable => new HttpRequestException(message),
            _ => new Exception(message)
        };
    }

    public async Task<List<Guid>> DeleteQuestions(DeleteQuestionsDto dto)
    {
        _log.Debug($"AssessmentOrchestrator.DeleteQuestions: count={dto.QuestionIds.Count}, createdBy={dto.CreatedBy}");

        var result = await _microServiceOrchestrator.DeleteQuestions(dto.ToMicroServiceModel());

        if (result.IsSuccess())
        {
            return result.DeserializeValue<List<Guid>>()
                ?? throw new InvalidOperationException("DeleteQuestions returned an empty response.");
        }

        var message = ExtractMessage(result.Value) ?? $"DeleteQuestions failed with status {result.StatusCode}.";

        throw result.StatusCode switch
        {
            StatusCodes.Status400BadRequest => new ArgumentException(message),
            StatusCodes.Status403Forbidden => new UnauthorizedAccessException(message),
            StatusCodes.Status404NotFound => new KeyNotFoundException(message),
            StatusCodes.Status409Conflict => new InvalidOperationException(message),
            StatusCodes.Status503ServiceUnavailable => new HttpRequestException(message),
            _ => new Exception(message)
        };
    }

    public async Task<PagedQuestionBankDto> SearchQuestionBank(QuestionBankSearchDto dto)
    {
        _log.Debug($"AssessmentOrchestrator.SearchQuestionBank: collegeId={dto.CollegeId}, subject={dto.Subject}, topic={dto.Topic}, difficultyLevel={dto.DifficultyLevel}, page={dto.PageNumber}, pageSize={dto.PageSize}");

        var result = await _microServiceOrchestrator.SearchQuestionBank(dto.ToMicroServiceModel());

        if (result.IsSuccess())
        {
            var model = result.DeserializeValue<PagedQuestionBankModel>()
                ?? throw new InvalidOperationException("SearchQuestionBank returned an empty response.");

            return model.ToDto();
        }

        var message = ExtractMessage(result.Value) ?? $"SearchQuestionBank failed with status {result.StatusCode}.";

        throw result.StatusCode switch
        {
            StatusCodes.Status400BadRequest => new ArgumentException(message),
            StatusCodes.Status503ServiceUnavailable => new HttpRequestException(message),
            _ => new Exception(message)
        };
    }

    public async Task<List<AssessmentDto>> GetAssessmentsByUser(string userId)
    {
        _log.Debug($"AssessmentOrchestrator.GetAssessmentsByUser: userId={userId}");

        var result = await _microServiceOrchestrator.GetAssessmentsByUser(userId);
        result.EnsureSuccess(nameof(GetAssessmentsByUser));

        List<AssessmentModel> models = result.DeserializeValue<List<AssessmentModel>>()
            ?? throw new InvalidOperationException($"GetAssessmentsByUser returned an empty response for userId={userId}.");

        return models.Select(a => a.ToDto()).ToList();
    }

    public async Task<AssessmentResultDto> GetAssessmentResult(string assessmentId, string userId)
    {
        _log.Debug($"AssessmentOrchestrator.GetAssessmentResult: assessmentId={assessmentId}, userId={userId}");

        var result = await _microServiceOrchestrator.GetAssessmentResult(assessmentId, userId);
        result.EnsureSuccess(nameof(GetAssessmentResult));

        AssessmentResultModel model = result.DeserializeValue<AssessmentResultModel>()
            ?? throw new InvalidOperationException($"GetAssessmentResult returned an empty response for assessmentId={assessmentId}, userId={userId}.");

        return model.ToDto();
    }

    public async Task<AssessmentSummaryDto> GetAssessmentSummary(string assessmentId)
    {
        _log.Debug($"AssessmentOrchestrator.GetAssessmentSummary: assessmentId={assessmentId}");

        var result = await _microServiceOrchestrator.GetAssessmentSummary(assessmentId);
        result.EnsureSuccess(nameof(GetAssessmentSummary));

        AssessmentSummaryModel model = result.DeserializeValue<AssessmentSummaryModel>()
            ?? throw new InvalidOperationException($"GetAssessmentSummary returned an empty response for assessmentId={assessmentId}.");

        return model.ToDto();
    }

    private static string? ExtractMessage(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is string json)
        {
            try
            {
                var parsed = JObject.Parse(json);
                return parsed["message"]?.ToString()
                    ?? parsed["Message"]?.ToString()
                    ?? json;
            }
            catch
            {
                return json;
            }
        }

        var token = JToken.FromObject(value);
        return token["message"]?.ToString() ?? token["Message"]?.ToString();
    }

    public async Task<PagedAssessmentQuestionListDto> GetAssessmentQuestionList(
        Guid assessmentId,
        int pageNumber,
        int pageSize)
    {
        _log.Debug($"AssessmentOrchestrator.GetAssessmentQuestionList: assessmentId={assessmentId}, page={pageNumber}, pageSize={pageSize}");

        var result = await _microServiceOrchestrator.GetAssessmentQuestionList(
            assessmentId,
            new AssessmentQuestionListSearchModel(pageNumber, pageSize));

        if (result.IsSuccess())
        {
            var pagedModel = result.DeserializeValue<PagedAssessmentQuestionListModel>()
                ?? throw new InvalidOperationException("GetAssessmentQuestionList returned an empty response.");

            return pagedModel.ToDto();
        }

        var message = ExtractMessage(result.Value) ?? $"GetAssessmentQuestionList failed with status {result.StatusCode}.";

        throw result.StatusCode switch
        {
            StatusCodes.Status400BadRequest         => new ArgumentException(message),
            StatusCodes.Status403Forbidden          => new UnauthorizedAccessException(message),
            StatusCodes.Status404NotFound           => new KeyNotFoundException(message),
            StatusCodes.Status503ServiceUnavailable => new HttpRequestException(message),
            _ => new Exception(message)
        };
    }
}
