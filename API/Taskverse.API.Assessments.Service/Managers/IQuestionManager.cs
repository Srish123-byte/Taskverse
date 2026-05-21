using Taskverse.Data.DataAccess;

namespace Taskverse.API.Assessments.Service.Managers;

public interface IQuestionManager
{
    Task<List<Question>> CreateQuestions(List<Question> questions);
    Task<Question> UpdateQuestion(Guid questionId, Question updatedQuestion);
    Task<List<Guid>> DeleteQuestions(string createdBy, List<Guid> questionIds);
}
