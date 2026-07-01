using Taskverse.API.Assessments.Service.Models;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.Assessments.Service.Managers;

/// <summary>
/// Defines question-bank operations handled by the assessments microservice.
/// </summary>
public interface IQuestionManager
{
    /// <summary>
    /// Creates one or more questions in the question bank.
    /// </summary>
    /// <param name="questions">The imported question rows to validate and save.</param>
    /// <returns>The saved questions.</returns>
    Task<List<QuestionRecord>> CreateQuestions(List<QuestionImportItem> questions);

    /// <summary>
    /// Returns the active subject-topic classification catalog for question creation flows.
    /// </summary>
    /// <returns>The available subjects and their topics.</returns>
    Task<QuestionClassificationCatalogRecord> GetQuestionClassificationCatalog();

    /// <summary>
    /// Creates or resolves a subject or topic entry for the shared classification catalog.
    /// </summary>
    /// <param name="request">The requested subject/topic details.</param>
    /// <returns>The persisted catalog entry.</returns>
    Task<QuestionClassificationEntryRecord> CreateQuestionClassificationEntry(CreateQuestionClassificationEntryRequest request);

    /// <summary>
    /// Retrieves a question by identifier within the specified college scope.
    /// </summary>
    /// <param name="collegeId">The college identifier.</param>
    /// <param name="questionId">The question identifier.</param>
    /// <returns>The requested question.</returns>
    Task<QuestionRecord> GetQuestionById(Guid collegeId, Guid questionId);

    /// <summary>
    /// Updates an existing question in the question bank.
    /// </summary>
    /// <param name="questionId">The question identifier.</param>
    /// <param name="request">The updated question payload.</param>
    /// <param name="requesterRole">The role of the caller performing the update.</param>
    /// <returns>The updated question.</returns>
    Task<QuestionRecord> UpdateQuestion(Guid questionId, CreateQuestionRequest request, string? requesterRole);

    /// <summary>
    /// Deletes one or more questions after enforcing role and assessment status restrictions.
    /// </summary>
    /// <param name="createdBy">The caller display name used for trainer ownership checks.</param>
    /// <param name="requesterRole">The role of the caller.</param>
    /// <param name="collegeId">The college scope for the request.</param>
    /// <param name="questionIds">The question identifiers to delete.</param>
    /// <returns>The deleted question identifiers.</returns>
    Task<List<Guid>> DeleteQuestions(string createdBy, string? requesterRole, Guid collegeId, List<Guid> questionIds);

    /// <summary>
    /// Searches the question bank using the supplied filters and paging settings.
    /// </summary>
    /// <param name="collegeId">The college identifier.</param>
    /// <param name="difficultyLevel">The optional difficulty filter.</param>
    /// <param name="subjectId">The optional subject identifier filter.</param>
    /// <param name="topicId">The optional topic identifier filter.</param>
    /// <param name="subject">The optional subject name filter.</param>
    /// <param name="topic">The optional topic name filter.</param>
    /// <param name="pageNumber">The requested page number.</param>
    /// <param name="pageSize">The requested page size.</param>
    /// <returns>The paged questions and the total match count.</returns>
    Task<(List<QuestionRecord> Items, int TotalCount)> SearchQuestionBank(
        Guid collegeId,
        int? difficultyLevel,
        Guid? subjectId,
        Guid? topicId,
        string? subject,
        string? topic,
        int pageNumber,
        int pageSize);
}
