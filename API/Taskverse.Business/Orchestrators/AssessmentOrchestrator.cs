using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Taskverse.Api.MicroServices.Interfaces;
using Taskverse.Api.MicroServices.Models;
using Taskverse.Api.MicroServices.Utilities;
using Taskverse.Business.DTOs;
using Taskverse.Business.Interface;
using Taskverse.Business.Mappings;
using Taskverse.Business.Utilities;

namespace Taskverse.Business.Orchestrators;

public class AssessmentOrchestrator : IAssessmentOrchestrator
{
    private readonly IMicroServiceOrchestrator _microServiceOrchestrator;
    private static readonly ILog _log = LogManager.GetLogger(typeof(AssessmentOrchestrator));

    public AssessmentOrchestrator(IMicroServiceOrchestrator microServiceOrchestrator)
    {
        _microServiceOrchestrator = microServiceOrchestrator;
    }

    public async Task<QuestionBankAssessmentDto> CreateAssessment(CreateQuestionBankAssessmentDto dto)
    {
        _log.Debug($"AssessmentOrchestrator.CreateAssessment: assessmentName={dto.AssessmentName}, collegeId={dto.CollegeId}, questionCount={dto.QuestionIds.Count}");

        ObjectResult result;
        try
        {
            result = await _microServiceOrchestrator.CreateAssessment(dto.ToMicroServiceModel());
        }
        catch (InvalidOperationException ex) when (string.Equals(ex.Message, MicroServiceBusinessCondition.AddressNotFound, StringComparison.Ordinal))
        {
            throw new HttpRequestException("Assessment microservice address is missing or invalid.", ex);
        }

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

    public async Task<QuestionBankAssessmentDto> GetAssessment(
        Guid assessmentId,
        Guid collegeId,
        string requesterRole,
        string requesterName)
    {
        _log.Debug(
            $"AssessmentOrchestrator.GetAssessment: assessmentId={assessmentId}, collegeId={collegeId}, requesterRole={requesterRole}");

        ObjectResult result;
        try
        {
            result = await _microServiceOrchestrator.GetAssessment(assessmentId, collegeId, requesterRole, requesterName);
        }
        catch (InvalidOperationException ex) when (string.Equals(ex.Message, MicroServiceBusinessCondition.AddressNotFound, StringComparison.Ordinal))
        {
            throw new HttpRequestException("Assessment microservice address is missing or invalid.", ex);
        }

        if (result.IsSuccess())
        {
            var model = result.DeserializeValue<QuestionBankAssessmentModel>()
                ?? throw new InvalidOperationException("GetAssessment returned an empty response.");

            return model.ToDto();
        }

        var message = ExtractMessage(result.Value) ?? $"GetAssessment failed with status {result.StatusCode}.";

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

    public async Task<QuestionBankAssessmentDto> UpdateAssessment(UpdateQuestionBankAssessmentDto dto)
    {
        _log.Debug(
            $"AssessmentOrchestrator.UpdateAssessment: assessmentId={dto.AssessmentId}, assessmentName={dto.AssessmentName}, collegeId={dto.CollegeId}, requesterRole={dto.RequesterRole}, questionCount={dto.QuestionIds.Count}");

        ObjectResult result;
        try
        {
            result = await _microServiceOrchestrator.UpdateAssessment(dto.ToMicroServiceModel());
        }
        catch (InvalidOperationException ex) when (string.Equals(ex.Message, MicroServiceBusinessCondition.AddressNotFound, StringComparison.Ordinal))
        {
            throw new HttpRequestException("Assessment microservice address is missing or invalid.", ex);
        }

        if (result.IsSuccess())
        {
            var model = result.DeserializeValue<QuestionBankAssessmentModel>()
                ?? throw new InvalidOperationException("UpdateAssessment returned an empty response.");

            return model.ToDto();
        }

        var message = ExtractMessage(result.Value) ?? $"UpdateAssessment failed with status {result.StatusCode}.";

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

    public async Task<QuestionBankAssessmentDto> PublishAssessment(PublishQuestionBankAssessmentDto dto)
    {
        _log.Debug(
            $"AssessmentOrchestrator.PublishAssessment: assessmentId={dto.AssessmentId}, assessmentName={dto.AssessmentName}, collegeId={dto.CollegeId}, questionCount={dto.QuestionIds.Count}");

        ObjectResult result;
        try
        {
            result = await _microServiceOrchestrator.PublishAssessment(dto.ToMicroServiceModel());
        }
        catch (InvalidOperationException ex) when (string.Equals(ex.Message, MicroServiceBusinessCondition.AddressNotFound, StringComparison.Ordinal))
        {
            throw new HttpRequestException("Assessment microservice address is missing or invalid.", ex);
        }

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

    public async Task DeleteAssessment(DeleteAssessmentDto dto)
    {
        _log.Debug(
            $"AssessmentOrchestrator.DeleteAssessment: assessmentId={dto.AssessmentId}, requesterRole={dto.RequesterRole}, collegeId={dto.CollegeId}");

        var result = await _microServiceOrchestrator.DeleteAssessment(dto.ToMicroServiceModel());

        if (result.IsSuccess())
        {
            return;
        }

        var message = ExtractMessage(result.Value) ?? $"DeleteAssessment failed with status {result.StatusCode}.";

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

    public async Task<AssessmentQuestionDto> GetQuestion(Guid questionId, Guid collegeId)
    {
        _log.Debug($"AssessmentOrchestrator.GetQuestion: questionId={questionId}, collegeId={collegeId}");

        var result = await _microServiceOrchestrator.GetQuestion(questionId, collegeId);

        if (result.IsSuccess())
        {
            var model = result.DeserializeValue<AssessmentQuestionModel>()
                ?? throw new InvalidOperationException("GetQuestion returned an empty response.");

            return model.ToDto();
        }

        var message = ExtractMessage(result.Value) ?? $"GetQuestion failed with status {result.StatusCode}.";

        throw result.StatusCode switch
        {
            StatusCodes.Status400BadRequest => new ArgumentException(message),
            StatusCodes.Status404NotFound => new KeyNotFoundException(message),
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

    public async Task<AssessmentSubjectTopicCatalogDto> GetSubjectTopicCatalog(AssessmentBootstrapDto dto)
    {
        _log.Debug(
            $"AssessmentOrchestrator.GetSubjectTopicCatalog: collegeId={dto.CollegeId}, requesterRole={dto.RequesterRole}, requesterUserId={dto.RequesterUserId}");

        var result = await _microServiceOrchestrator.GetSubjectTopicCatalog(dto.ToMicroServiceModel());

        if (result.IsSuccess())
        {
            var model = result.DeserializeValue<AssessmentSubjectTopicCatalogModel>()
                ?? throw new InvalidOperationException("GetSubjectTopicCatalog returned an empty response.");

            return model.ToDto();
        }

        var message = ExtractMessage(result.Value) ?? $"GetSubjectTopicCatalog failed with status {result.StatusCode}.";
        var detail = ExtractDetail(result.Value);

        Exception exception = result.StatusCode switch
        {
            StatusCodes.Status400BadRequest => new ArgumentException(message),
            StatusCodes.Status503ServiceUnavailable => new HttpRequestException(message),
            _ => new Exception(message)
        };

        if (!string.IsNullOrWhiteSpace(detail))
        {
            exception.Data["Detail"] = detail;
        }

        exception.Data["DownstreamStatusCode"] = result.StatusCode;

        throw exception;
    }

    public async Task<AssessmentAssignmentCatalogDto> GetTrainerAssignedClassesAndBatches(AssessmentBootstrapDto dto)
    {
        _log.Debug(
            $"AssessmentOrchestrator.GetTrainerAssignedClassesAndBatches: collegeId={dto.CollegeId}, requesterRole={dto.RequesterRole}, requesterUserId={dto.RequesterUserId}");

        var result = await _microServiceOrchestrator.GetTrainerAssignedClassesAndBatches(dto.ToMicroServiceModel());

        if (result.IsSuccess())
        {
            var model = result.DeserializeValue<AssessmentAssignmentCatalogModel>()
                ?? throw new InvalidOperationException("GetTrainerAssignedClassesAndBatches returned an empty response.");

            return model.ToDto();
        }

        var message = ExtractMessage(result.Value) ?? $"GetTrainerAssignedClassesAndBatches failed with status {result.StatusCode}.";

        throw result.StatusCode switch
        {
            StatusCodes.Status400BadRequest => new ArgumentException(message),
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

    public async Task<AssessmentManagementSearchResultDto> SearchAssessments(AssessmentManagementSearchDto dto)
    {
        _log.Debug(
            $"AssessmentOrchestrator.SearchAssessments: collegeId={dto.CollegeId}, requesterRole={dto.RequesterRole}, search={dto.SearchTerm}, status={dto.AssessmentStatus}, difficultyLevel={dto.DifficultyLevel}, page={dto.PageNumber}, pageSize={dto.PageSize}");

        var result = await _microServiceOrchestrator.SearchAssessments(dto.ToMicroServiceModel());

        if (result.IsSuccess())
        {
            var model = result.DeserializeValue<AssessmentManagementSearchResultModel>()
                ?? throw new InvalidOperationException("SearchAssessments returned an empty response.");

            return model.ToDto();
        }

        var message = ExtractMessage(result.Value) ?? $"SearchAssessments failed with status {result.StatusCode}.";

        throw result.StatusCode switch
        {
            StatusCodes.Status400BadRequest => new ArgumentException(message),
            StatusCodes.Status503ServiceUnavailable => new HttpRequestException(message),
            _ => new Exception(message)
        };
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

    public async Task<List<StudentAssessmentListItemDto>> GetStudentAssessments(
        Guid studentUserId,
        IReadOnlyCollection<string> assessmentStatuses)
    {
        _log.Debug(
            $"AssessmentOrchestrator.GetStudentAssessments: studentUserId={studentUserId}, statuses={string.Join(",", assessmentStatuses)}");

        var result = await _microServiceOrchestrator.GetStudentAssessments(
            new StudentAssessmentListSearchModel(studentUserId),
            assessmentStatuses);

        if (result.IsSuccess())
        {
            var models = result.DeserializeValue<List<StudentAssessmentListItemModel>>()
                ?? throw new InvalidOperationException("GetStudentAssessments returned an empty response.");

            return models.Select(model => model.ToDto()).ToList();
        }

        var extractedMessage = ExtractMessage(result.Value);
        var extractedDetail = ExtractDetail(result.Value);
        var message = string.IsNullOrWhiteSpace(extractedDetail) || string.Equals(extractedMessage, extractedDetail, StringComparison.Ordinal)
            ? extractedMessage ?? extractedDetail ?? $"GetStudentAssessments failed with status {result.StatusCode}."
            : $"{extractedMessage ?? "GetStudentAssessments failed."} Detail: {extractedDetail}";

        throw result.StatusCode switch
        {
            StatusCodes.Status400BadRequest => new ArgumentException(message),
            StatusCodes.Status404NotFound => new KeyNotFoundException(message),
            StatusCodes.Status503ServiceUnavailable => new HttpRequestException(message),
            _ => new InvalidOperationException(message)
        };
    }

    public async Task<StudentAssessmentDetailDto> GetStudentAssessmentDetail(Guid assessmentId, Guid studentUserId)
    {
        _log.Debug(
            $"AssessmentOrchestrator.GetStudentAssessmentDetail: assessmentId={assessmentId}, studentUserId={studentUserId}");

        var result = await _microServiceOrchestrator.GetStudentAssessmentDetail(assessmentId, studentUserId);

        if (result.IsSuccess())
        {
            var model = result.DeserializeValue<StudentAssessmentDetailModel>()
                ?? throw new InvalidOperationException("GetStudentAssessmentDetail returned an empty response.");

            return model.ToDto();
        }

        var message = ExtractMessage(result.Value) ?? $"GetStudentAssessmentDetail failed with status {result.StatusCode}.";

        throw result.StatusCode switch
        {
            StatusCodes.Status400BadRequest => new ArgumentException(message),
            StatusCodes.Status404NotFound => new KeyNotFoundException(message),
            StatusCodes.Status503ServiceUnavailable => new HttpRequestException(message),
            _ => new InvalidOperationException(message)
        };
    }

    public async Task<StudentAssessmentStartDto> StartStudentAssessment(Guid assessmentId, Guid studentUserId)
    {
        _log.Debug(
            $"AssessmentOrchestrator.StartStudentAssessment: assessmentId={assessmentId}, studentUserId={studentUserId}");

        var result = await _microServiceOrchestrator.StartStudentAssessment(assessmentId, studentUserId);

        if (result.IsSuccess())
        {
            var model = result.DeserializeValue<StudentAssessmentStartModel>()
                ?? throw new InvalidOperationException("StartStudentAssessment returned an empty response.");

            return model.ToDto();
        }

        var message = ExtractMessage(result.Value) ?? $"StartStudentAssessment failed with status {result.StatusCode}.";

        throw result.StatusCode switch
        {
            StatusCodes.Status400BadRequest => new ArgumentException(message),
            StatusCodes.Status404NotFound => new KeyNotFoundException(message),
            StatusCodes.Status409Conflict => new InvalidOperationException(message),
            StatusCodes.Status503ServiceUnavailable => new HttpRequestException(message),
            _ => new InvalidOperationException(message)
        };
    }

    public async Task<StudentAttemptRecoveryDto> GetStudentAttemptRecovery(Guid attemptId, Guid studentUserId)
    {
        _log.Debug(
            $"AssessmentOrchestrator.GetStudentAttemptRecovery: attemptId={attemptId}, studentUserId={studentUserId}");

        var result = await _microServiceOrchestrator.GetStudentAttemptRecovery(attemptId, studentUserId);

        if (result.IsSuccess())
        {
            var model = result.DeserializeValue<StudentAttemptRecoveryModel>()
                ?? throw new InvalidOperationException("GetStudentAttemptRecovery returned an empty response.");

            return model.ToDto();
        }

        var message = ExtractMessage(result.Value) ?? $"GetStudentAttemptRecovery failed with status {result.StatusCode}.";

        throw result.StatusCode switch
        {
            StatusCodes.Status400BadRequest => new ArgumentException(message),
            StatusCodes.Status404NotFound => new KeyNotFoundException(message),
            StatusCodes.Status409Conflict => new InvalidOperationException(message),
            StatusCodes.Status503ServiceUnavailable => new HttpRequestException(message),
            _ => new InvalidOperationException(message)
        };
    }

    public async Task<StudentAttemptAnswerDto> SaveStudentAttemptAnswer(
        Guid attemptId,
        Guid questionId,
        Guid studentUserId,
        SaveStudentAttemptAnswerDto dto)
    {
        _log.Debug(
            $"AssessmentOrchestrator.SaveStudentAttemptAnswer: attemptId={attemptId}, studentUserId={studentUserId}, questionId={questionId}");

        var result = await _microServiceOrchestrator.SaveStudentAttemptAnswer(
            attemptId,
            questionId,
            studentUserId,
            dto.ToMicroServiceModel());

        if (result.IsSuccess())
        {
            var model = result.DeserializeValue<StudentAttemptAnswerModel>()
                ?? throw new InvalidOperationException("SaveStudentAttemptAnswer returned an empty response.");

            return model.ToDto();
        }

        var message = ExtractMessage(result.Value) ?? $"SaveStudentAttemptAnswer failed with status {result.StatusCode}.";

        throw result.StatusCode switch
        {
            StatusCodes.Status400BadRequest => new ArgumentException(message),
            StatusCodes.Status404NotFound => new KeyNotFoundException(message),
            StatusCodes.Status409Conflict => new InvalidOperationException(message),
            StatusCodes.Status503ServiceUnavailable => new HttpRequestException(message),
            _ => new InvalidOperationException(message)
        };
    }

    public async Task<StudentAttemptSubmitDto> SubmitStudentAttempt(Guid attemptId, Guid studentUserId)
    {
        _log.Debug(
            $"AssessmentOrchestrator.SubmitStudentAttempt: attemptId={attemptId}, studentUserId={studentUserId}");

        var result = await _microServiceOrchestrator.SubmitStudentAttempt(attemptId, studentUserId);

        if (result.IsSuccess())
        {
            var model = result.DeserializeValue<StudentAttemptSubmitModel>()
                ?? throw new InvalidOperationException("SubmitStudentAttempt returned an empty response.");

            return model.ToDto();
        }

        var message = ExtractMessage(result.Value) ?? $"SubmitStudentAttempt failed with status {result.StatusCode}.";

        throw result.StatusCode switch
        {
            StatusCodes.Status400BadRequest => new ArgumentException(message),
            StatusCodes.Status404NotFound => new KeyNotFoundException(message),
            StatusCodes.Status409Conflict => new InvalidOperationException(message),
            StatusCodes.Status503ServiceUnavailable => new HttpRequestException(message),
            _ => new InvalidOperationException(message)
        };
    }

    private static string? ExtractDetail(object? value)
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
                return parsed["detail"]?.ToString()
                    ?? parsed["Detail"]?.ToString();
            }
            catch
            {
                return null;
            }
        }

        var token = JToken.FromObject(value);
        return token["detail"]?.ToString() ?? token["Detail"]?.ToString();
    }
}
