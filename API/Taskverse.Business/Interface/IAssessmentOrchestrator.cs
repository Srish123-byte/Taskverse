using Taskverse.Business.DTOs;

namespace Taskverse.Business.Interface;

public interface IAssessmentOrchestrator
{
    Task<AssessmentDto?> GetAssessment(string assessmentId);
    Task<AssessmentDto?> CreateAssessment(CreateAssessmentDto dto);
    Task<List<AssessmentQuestionDto>> CreateQuestions(List<CreateQuestionDto> dtos);
    Task<AssessmentQuestionDto> UpdateQuestion(Guid questionId, CreateQuestionDto dto);
    Task<List<Guid>> DeleteQuestions(DeleteQuestionsDto dto);
    Task<List<AssessmentDto>?> GetAssessmentsByUser(string userId);
    Task<AssessmentResultDto?> GetAssessmentResult(string assessmentId, string userId);
    Task<AssessmentSummaryDto?> GetAssessmentSummary(string assessmentId);
}
