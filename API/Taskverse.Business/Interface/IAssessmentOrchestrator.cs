using Taskverse.Business.DTOs;

namespace Taskverse.Business.Interface;

public interface IAssessmentOrchestrator
{
    Task<QuestionBankAssessmentDto> CreateAssessment(CreateQuestionBankAssessmentDto dto);
    Task<QuestionBankAssessmentDto> PublishAssessment(PublishQuestionBankAssessmentDto dto);
    Task DeleteAssessment(DeleteAssessmentDto dto);
    Task<QuestionBankAssessmentDto> PublishAssessment(Guid assessmentId);
    Task<List<AssessmentQuestionDto>> CreateQuestions(List<CreateQuestionDto> dtos);
    Task<AssessmentQuestionDto> GetQuestion(Guid questionId, Guid collegeId);
    Task<AssessmentQuestionDto> UpdateQuestion(Guid questionId, CreateQuestionDto dto);
    Task<List<Guid>> DeleteQuestions(DeleteQuestionsDto dto);
    Task<AssessmentSubjectTopicCatalogDto> GetSubjectTopicCatalog(AssessmentBootstrapDto dto);
    Task<AssessmentAssignmentCatalogDto> GetTrainerAssignedClassesAndBatches(AssessmentBootstrapDto dto);
    Task<PagedQuestionBankDto> SearchQuestionBank(QuestionBankSearchDto dto);
    Task<AssessmentManagementSearchResultDto> SearchAssessments(AssessmentManagementSearchDto dto);
    Task<PagedAssessmentQuestionListDto> GetAssessmentQuestionList(Guid assessmentId, int pageNumber, int pageSize);
    Task<List<StudentAssessmentListItemDto>> GetStudentAssessments(Guid studentUserId, IReadOnlyCollection<string> assessmentStatuses);
    Task<StudentAssessmentDetailDto> GetStudentAssessmentDetail(Guid assessmentId, Guid studentUserId);
    Task<StudentAssessmentStartDto> StartStudentAssessment(Guid assessmentId, Guid studentUserId);
    Task<StudentAttemptRecoveryDto> GetStudentAttemptRecovery(Guid attemptId, Guid studentUserId);
    Task<StudentAttemptAnswerDto> SaveStudentAttemptAnswer(Guid attemptId, Guid questionId, Guid studentUserId, SaveStudentAttemptAnswerDto dto);
    Task<StudentAttemptSubmitDto> SubmitStudentAttempt(Guid attemptId, Guid studentUserId);
}
