using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Taskverse.API.CodingEngine.Service.Managers;
using Taskverse.API.CodingEngine.Service.Models;
using Taskverse.API.CodingEngine.Service.Services;
using Taskverse.Data.DataAccess;
using Taskverse.Data.Enums;

namespace Taskverse.API.CodingEngine.Service.Orchestrators;

public class CodingEngineOrchestrator : ICodingEngineOrchestrator
{
    private static readonly TimeSpan InlineWaitBudget = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan InlineWaitPollInterval = TimeSpan.FromMilliseconds(250);

    private readonly ICodingEngineManager _codingEngineManager;
    private readonly IDispatchService _dispatchService;
    private readonly IPollService _pollService;
    private readonly IExecutionLifecycleService _executionLifecycle;
    private readonly ILogger<CodingEngineOrchestrator> _logger;

    public CodingEngineOrchestrator(
        ICodingEngineManager codingEngineManager,
        IDispatchService dispatchService,
        IPollService pollService,
        IExecutionLifecycleService executionLifecycle,
        ILogger<CodingEngineOrchestrator> logger)
    {
        _codingEngineManager = codingEngineManager;
        _dispatchService = dispatchService;
        _pollService = pollService;
        _executionLifecycle = executionLifecycle;
        _logger = logger;
    }

    public async Task<EditorStateResponse> GetEditorStateAsync(Guid assessmentId, Guid codingQuestionId, Guid studentUserId)
    {
        ValidateGuid(assessmentId, nameof(assessmentId));
        ValidateGuid(codingQuestionId, nameof(codingQuestionId));
        ValidateGuid(studentUserId, nameof(studentUserId));

        return await ExecuteDbOperationAsync(async () =>
        {
            var student = await GetStudentByUserIdAsync(studentUserId);
            var assessmentCodingQuestion = await GetAssessmentCodingQuestionAsync(assessmentId, codingQuestionId);

            var codingQuestion = assessmentCodingQuestion.CodingQuestion;

            var settings = await _codingEngineManager.GetCodingSettingAsync(assessmentId);
            var settingsDetail = settings is not null
                ? new CodingSettingsDetail(
                    settings.TimeLimitMs,
                    settings.MemoryLimitKb,
                    settings.MaxCodeSizeKb,
                    settings.IsCodeExecutionEnabled,
                    settings.IsSubmissionEnabled,
                    settings.AllowLanguageChange)
                : new CodingSettingsDetail(3000, 262144, 512, false, true, true);

            var starterCodes = await _codingEngineManager.GetStarterCodesByQuestionAsync(codingQuestionId);
            var starterCodeDetails = starterCodes
                .Select(sc => new StarterCodeDetail(sc.CodingLanguageId, sc.StarterCodeContent))
                .ToList();

            var languages = await _codingEngineManager.GetAvailableLanguagesAsync();
            var languageDetails = languages
                .Select(l => new AvailableLanguage(
                    l.CodingLanguageId,
                    l.LanguageCode,
                    l.DisplayName,
                    l.MonacoLanguageCode,
                    l.FileExtension))
                .ToList();

            string? studentCode = null;
            Guid? selectedLanguageId = null;

            var defaultLanguage = languages.FirstOrDefault(l =>
                l.LanguageCode == codingQuestion.DefaultLanguageCode);

            if (defaultLanguage is not null)
            {
                selectedLanguageId = defaultLanguage.CodingLanguageId;

                var existingCode = await _codingEngineManager.GetStudentCodeAsync(
                    student.StudentId,
                    assessmentId,
                    codingQuestionId,
                    defaultLanguage.CodingLanguageId);

                if (existingCode is not null)
                {
                    studentCode = existingCode.Code;
                    selectedLanguageId = existingCode.CodingLanguageId;
                }
            }

            var questionDetail = new CodingQuestionDetail(
                codingQuestion.CodingQuestionId,
                codingQuestion.QuestionTitle,
                codingQuestion.ProblemStatement,
                codingQuestion.DetailedDescription,
                codingQuestion.DifficultyLevel,
                codingQuestion.InputFormat,
                codingQuestion.OutputFormat,
                codingQuestion.ConstraintsText,
                codingQuestion.Explanation,
                codingQuestion.Examples,
                codingQuestion.Marks,
                codingQuestion.DefaultLanguageCode);

            return new EditorStateResponse(
                questionDetail,
                languageDetails,
                starterCodeDetails,
                studentCode,
                selectedLanguageId,
                settingsDetail);
        }, "retrieving the coding editor state");
    }

    public async Task<SaveCodeResponse> SaveCodeAsync(Guid assessmentId, Guid codingQuestionId, Guid studentUserId, SaveCodeRequest request)
    {
        ValidateGuid(assessmentId, nameof(assessmentId));
        ValidateGuid(codingQuestionId, nameof(codingQuestionId));
        ValidateGuid(studentUserId, nameof(studentUserId));
        ValidateSaveCodeRequest(request);

        return await ExecuteDbOperationAsync(async () =>
        {
            var student = await GetStudentByUserIdAsync(studentUserId);
            await GetAssessmentCodingQuestionAsync(assessmentId, codingQuestionId);

            var now = DateTime.UtcNow;
            var studentCode = new StudentCode
            {
                StudentCodeId = Guid.NewGuid(),
                StudentId = student.StudentId,
                AssessmentId = assessmentId,
                CodingQuestionId = codingQuestionId,
                CodingLanguageId = request.CodingLanguageId,
                Code = request.Code,
                LastSavedAt = now,
                CreatedAt = now,
                ModifiedAt = now
            };

            _codingEngineManager.AddStudentCode(studentCode);
            await SaveChangesWithWrapAsync("Unable to save the student code.");

            return new SaveCodeResponse("saved", now);
        }, "saving the student code");
    }

    public async Task<RunCodeResponse> RunCodeAsync(
        Guid assessmentId, Guid codingQuestionId, Guid studentUserId, RunCodeRequest request, CancellationToken cancellationToken = default)
    {
        ValidateGuid(assessmentId, nameof(assessmentId));
        ValidateGuid(codingQuestionId, nameof(codingQuestionId));
        ValidateGuid(studentUserId, nameof(studentUserId));
        ValidateRunCodeRequest(request);

        return await ExecuteDbOperationAsync(async () =>
        {
            var student = await GetStudentByUserIdAsync(studentUserId);
            await GetAssessmentCodingQuestionAsync(assessmentId, codingQuestionId);

            var now = DateTime.UtcNow;
            var executionMode = "Run";

            var studentCode = new StudentCode
            {
                StudentCodeId = Guid.NewGuid(),
                StudentId = student.StudentId,
                AssessmentId = assessmentId,
                CodingQuestionId = codingQuestionId,
                CodingLanguageId = request.CodingLanguageId,
                Code = request.Code,
                LastSavedAt = now,
                CreatedAt = now,
                ModifiedAt = now
            };

            _codingEngineManager.AddStudentCode(studentCode);

            var executionRequest = new CodeExecutionRequest
            {
                CodeExecutionRequestId = Guid.NewGuid(),
                StudentId = student.StudentId,
                AssessmentId = assessmentId,
                CodingLanguageId = request.CodingLanguageId,
                Code = request.Code,
                InputPayload = null,
                ExecutionMode = executionMode,
                CodeExecutionStatusId = (short)CodeExecutionStatus.Queued,
                RequestedAt = now,
                CreatedAt = now,
                ModifiedAt = now
            };

            _codingEngineManager.AddCodeExecutionRequest(executionRequest);
            await SaveChangesWithWrapAsync("Unable to queue the code execution.");

            return new RunCodeResponse(
                executionRequest.CodeExecutionRequestId,
                "Queued",
                null,
                0,
                0,
                null);
        }, "queuing the code execution");
    }

    public async Task<RunCodeResponse> SubmitCodeAsync(
        Guid assessmentId, Guid codingQuestionId, Guid studentUserId, RunCodeRequest request, CancellationToken cancellationToken = default)
    {
        ValidateGuid(assessmentId, nameof(assessmentId));
        ValidateGuid(codingQuestionId, nameof(codingQuestionId));
        ValidateGuid(studentUserId, nameof(studentUserId));
        ValidateRunCodeRequest(request);

        return await ExecuteDbOperationAsync(async () =>
        {
            var student = await GetStudentByUserIdAsync(studentUserId);
            await GetAssessmentCodingQuestionAsync(assessmentId, codingQuestionId);

            var now = DateTime.UtcNow;
            var executionMode = "Submit";

            var studentCode = new StudentCode
            {
                StudentCodeId = Guid.NewGuid(),
                StudentId = student.StudentId,
                AssessmentId = assessmentId,
                CodingQuestionId = codingQuestionId,
                CodingLanguageId = request.CodingLanguageId,
                Code = request.Code,
                LastSavedAt = now,
                CreatedAt = now,
                ModifiedAt = now
            };

            _codingEngineManager.AddStudentCode(studentCode);

            var executionRequest = new CodeExecutionRequest
            {
                CodeExecutionRequestId = Guid.NewGuid(),
                StudentId = student.StudentId,
                AssessmentId = assessmentId,
                CodingLanguageId = request.CodingLanguageId,
                Code = request.Code,
                InputPayload = null,
                ExecutionMode = executionMode,
                CodeExecutionStatusId = (short)CodeExecutionStatus.Queued,
                RequestedAt = now,
                CreatedAt = now,
                ModifiedAt = now
            };

            _codingEngineManager.AddCodeExecutionRequest(executionRequest);
            await SaveChangesWithWrapAsync("Unable to queue the code submission.");

            return new RunCodeResponse(
                executionRequest.CodeExecutionRequestId,
                "Queued",
                null,
                0,
                0,
                null);
        }, "queuing the code submission");
    }

    public async Task<RunCodeResponse> GetExecutionStatusAsync(Guid assessmentId, Guid executionRequestId, Guid studentUserId)
    {
        ValidateGuid(assessmentId, nameof(assessmentId));
        ValidateGuid(executionRequestId, nameof(executionRequestId));
        ValidateGuid(studentUserId, nameof(studentUserId));

        return await ExecuteDbOperationAsync(async () =>
        {
            var student = await GetStudentByUserIdAsync(studentUserId);

            var executionRequest = await _codingEngineManager.GetCodeExecutionRequestAsync(executionRequestId);
            if (executionRequest is null || executionRequest.AssessmentId != assessmentId || executionRequest.StudentId != student.StudentId)
            {
                throw new KeyNotFoundException($"Code execution '{executionRequestId}' was not found for this assessment.");
            }

            if (!IsTerminalStatus(executionRequest.CodeExecutionStatusId))
            {
                return new RunCodeResponse(
                    executionRequest.CodeExecutionRequestId,
                    ((CodeExecutionStatus)executionRequest.CodeExecutionStatusId).ToString(),
                    null, 0, 0, null);
            }

            return await BuildRunCodeResponseAsync(executionRequest);
        }, "retrieving the code execution status");
    }

    private async Task<RunCodeResponse> BuildRunCodeResponseAsync(CodeExecutionRequest executionRequest)
    {
        var status = ((CodeExecutionStatus)executionRequest.CodeExecutionStatusId).ToString();

        var result = await _codingEngineManager.GetCodeExecutionResultAsync(executionRequest.CodeExecutionRequestId);
        var submissions = await _codingEngineManager.GetCodeExecutionSubmissionsAsync(executionRequest.CodeExecutionRequestId);

        List<TestCaseResult>? testCaseResults = null;
        if (submissions.Count > 0)
        {
            var testCases = await _codingEngineManager.GetTestCasesByIdsAsync(
                submissions.Select(s => s.TestCaseId).Distinct().ToList());
            var testCasesById = testCases.ToDictionary(tc => tc.TestCaseId);

            testCaseResults = submissions
                .Select(submission =>
                {
                    testCasesById.TryGetValue(submission.TestCaseId, out var testCase);
                    return new TestCaseResult(
                        submission.TestCaseId,
                        testCase?.IsSample ?? false,
                        submission.Passed,
                        submission.ActualOutput,
                        testCase?.ExpectedOutput,
                        submission.ExecutionTimeMs,
                        submission.Judge0StatusDescription ?? (submission.Passed ? "Passed" : "Failed"));
                })
                .ToList();
        }

        return new RunCodeResponse(
            executionRequest.CodeExecutionRequestId,
            status,
            testCaseResults,
            result?.TotalTestCases ?? testCaseResults?.Count ?? 0,
            result?.PassedTestCases ?? testCaseResults?.Count(r => r.Passed) ?? 0,
            result?.CodingScore);
    }

    private static bool IsTerminalStatus(short statusId)
    {
        var status = (CodeExecutionStatus)statusId;
        return status is CodeExecutionStatus.Completed
            or CodeExecutionStatus.Failed
            or CodeExecutionStatus.Cancelled
            or CodeExecutionStatus.Timeout;
    }

    private static void ValidateGuid(Guid id, string paramName)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException($"{paramName} is required.");
        }
    }

    private static void ValidateSaveCodeRequest(SaveCodeRequest request)
    {
        if (request is null)
        {
            throw new ArgumentException("Save code request is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new ArgumentException("Code is required.");
        }

        if (request.CodingLanguageId == Guid.Empty)
        {
            throw new ArgumentException("Coding language id is required.");
        }
    }

    private static void ValidateRunCodeRequest(RunCodeRequest request)
    {
        if (request is null)
        {
            throw new ArgumentException("Run code request is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new ArgumentException("Code is required.");
        }

        if (request.CodingLanguageId == Guid.Empty)
        {
            throw new ArgumentException("Coding language id is required.");
        }
    }

    private static string NormalizeMode(string? mode)
        => string.Equals(mode, "Submit", StringComparison.OrdinalIgnoreCase) ? "Submit" : "Run";

    private async Task<Student> GetStudentByUserIdAsync(Guid studentUserId)
    {
        var student = await _codingEngineManager.GetStudentByUserIdAsync(studentUserId);
        return student ?? throw new KeyNotFoundException($"Student profile was not found for user '{studentUserId}'.");
    }

    private async Task<AssessmentCodingQuestion> GetAssessmentCodingQuestionAsync(Guid assessmentId, Guid codingQuestionId)
    {
        var assessmentCodingQuestion = await _codingEngineManager.GetAssessmentCodingQuestionAsync(assessmentId, codingQuestionId);
        return assessmentCodingQuestion ?? throw new KeyNotFoundException(
            $"Coding question '{codingQuestionId}' is not linked to assessment '{assessmentId}'.");
    }

    private async Task SaveChangesWithWrapAsync(string errorMessage)
    {
        try
        {
            await _codingEngineManager.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "{ErrorMessage} SaveChanges failed in CodingEngineOrchestrator.", errorMessage);
            throw new InvalidOperationException(errorMessage, ex);
        }
    }

    private async Task<T> ExecuteDbOperationAsync<T>(Func<Task<T>> operation, string operationName)
    {
        try
        {
            return await operation();
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while {OperationName}.", operationName);
            throw new InvalidOperationException($"An unexpected error occurred while {operationName}.", ex);
        }
    }
}
