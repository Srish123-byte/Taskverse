using Taskverse.Data.DataAccess;

namespace Taskverse.API.Assessments.Service.Managers;

public interface IQuestionManager
{
    Task<List<Question>> CreateQuestions(List<QuestionImportItem> questions);
    Task<Question> GetQuestionById(Guid collegeId, Guid questionId);
    Task<Question> UpdateQuestion(Guid questionId, Question updatedQuestion, string? requesterRole);
    Task<List<Guid>> DeleteQuestions(string createdBy, string? requesterRole, Guid collegeId, List<Guid> questionIds);
    Task<(List<Question> Items, int TotalCount)> SearchQuestionBank(
        Guid collegeId,
        int? difficultyLevel,
        Guid? subjectId,
        Guid? topicId,
        string? subject,
        string? topic,
        int pageNumber,
        int pageSize);
}
