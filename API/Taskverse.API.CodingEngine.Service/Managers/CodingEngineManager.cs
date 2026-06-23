using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.CodingEngine.Service.Managers;

public class CodingEngineManager : ICodingEngineManager
{
    private readonly TaskverseContext _context;
    private readonly ILogger<CodingEngineManager> _logger;

    public CodingEngineManager(TaskverseContext context, ILogger<CodingEngineManager> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AssessmentCodingQuestion?> GetAssessmentCodingQuestionAsync(Guid assessmentId, Guid codingQuestionId)
        => await ExecuteQueryAsync(
            () => _context.AssessmentCodingQuestions
                .Include(acq => acq.CodingQuestion)
                .FirstOrDefaultAsync(acq =>
                    acq.AssessmentId == assessmentId &&
                    acq.CodingQuestionId == codingQuestionId),
            $"retrieving assessment coding question for assessment '{assessmentId}' and question '{codingQuestionId}'.");

    public async Task<CodingQuestion?> GetCodingQuestionAsync(Guid codingQuestionId)
        => await ExecuteQueryAsync(
            () => _context.CodingQuestions
                .FirstOrDefaultAsync(cq => cq.CodingQuestionId == codingQuestionId),
            $"retrieving coding question '{codingQuestionId}'.");

    public async Task<CodingSetting?> GetCodingSettingAsync(Guid assessmentId)
        => await ExecuteQueryAsync(
            () => _context.CodingSettings
                .FirstOrDefaultAsync(),
            "retrieving coding settings.");

    public async Task<List<StarterCode>> GetStarterCodesByQuestionAsync(Guid codingQuestionId)
        => await ExecuteQueryAsync(
            () => _context.StarterCodes
                .Where(sc => sc.CodingQuestionId == codingQuestionId && sc.IsActive)
                .ToListAsync(),
            $"retrieving starter codes for question '{codingQuestionId}'.");

    public async Task<StudentCode?> GetStudentCodeAsync(Guid studentId, Guid assessmentId, Guid codingQuestionId, Guid codingLanguageId)
        => await ExecuteQueryAsync(
            () => _context.StudentCodes
                .OrderByDescending(sc => sc.LastSavedAt)
                .FirstOrDefaultAsync(sc =>
                    sc.StudentId == studentId &&
                    sc.AssessmentId == assessmentId &&
                    sc.CodingQuestionId == codingQuestionId &&
                    sc.CodingLanguageId == codingLanguageId),
            $"retrieving student code for student '{studentId}', assessment '{assessmentId}', question '{codingQuestionId}'.");

    public async Task<List<CodingLanguage>> GetAvailableLanguagesAsync()
        => await ExecuteQueryAsync(
            () => _context.CodingLanguages
                .Where(cl => cl.IsActive)
                .ToListAsync(),
            "retrieving available coding languages.");

    public async Task<Student?> GetStudentByUserIdAsync(Guid studentUserId)
        => await ExecuteQueryAsync(
            () => _context.Students.FirstOrDefaultAsync(s => s.UserId == studentUserId),
            $"retrieving student profile for user '{studentUserId}'.");

    public async Task<Attempt?> GetAttemptForStudentAsync(Guid assessmentId, Guid studentId)
        => await ExecuteQueryAsync(
            () => _context.Attempts
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync(a =>
                    a.AssessmentId == assessmentId &&
                    a.StudentId == studentId),
            $"retrieving attempt for assessment '{assessmentId}' and student '{studentId}'.");

    public void AddStudentCode(StudentCode studentCode)
        => ExecuteCommand(
            () => _context.StudentCodes.Add(studentCode),
            $"staging student code for student '{studentCode.StudentId}'.");

    public void AddCodeExecutionRequest(CodeExecutionRequest executionRequest)
        => ExecuteCommand(
            () => _context.CodeExecutionRequests.Add(executionRequest),
            $"staging code execution request '{executionRequest.CodeExecutionRequestId}'.");

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await ExecuteQueryAsync(
            () => _context.SaveChangesAsync(cancellationToken),
            "saving coding engine changes.");

    private async Task<T> ExecuteQueryAsync<T>(Func<Task<T>> operation, string operationDescription)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while {OperationDescription}", operationDescription);
            throw;
        }
    }

    private void ExecuteCommand(Action operation, string operationDescription)
    {
        try
        {
            operation();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while {OperationDescription}", operationDescription);
            throw;
        }
    }
}
