using Taskverse.Data.DataAccess;

namespace Taskverse.API.CodingEngine.Service.Managers;

public interface ICodingEngineManager
{
    Task<AssessmentCodingQuestion?> GetAssessmentCodingQuestionAsync(Guid assessmentId, Guid codingQuestionId);
    Task<CodingQuestion?> GetCodingQuestionAsync(Guid codingQuestionId);
    Task<CodingSetting?> GetCodingSettingAsync(Guid assessmentId);
    Task<List<StarterCode>> GetStarterCodesByQuestionAsync(Guid codingQuestionId);
    Task<StudentCode?> GetStudentCodeAsync(Guid studentId, Guid assessmentId, Guid codingQuestionId, Guid codingLanguageId);
    Task<List<CodingLanguage>> GetAvailableLanguagesAsync();
    Task<Student?> GetStudentByUserIdAsync(Guid studentUserId);
    Task<Attempt?> GetAttemptForStudentAsync(Guid assessmentId, Guid studentId);
    Task<CodeExecutionRequest?> GetCodeExecutionRequestAsync(Guid codeExecutionRequestId);
    Task<CodeExecutionResult?> GetCodeExecutionResultAsync(Guid codeExecutionRequestId);
    Task<List<CodeExecutionSubmission>> GetCodeExecutionSubmissionsAsync(Guid codeExecutionRequestId);
    Task<List<TestCase>> GetTestCasesByIdsAsync(List<Guid> testCaseIds);
    void AddStudentCode(StudentCode studentCode);
    void AddCodeExecutionRequest(CodeExecutionRequest executionRequest);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
