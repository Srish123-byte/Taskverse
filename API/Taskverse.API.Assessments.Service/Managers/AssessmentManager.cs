using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Taskverse.API.Assessments.Service.Models;
using Taskverse.Business.Enums;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.Assessments.Service.Managers;

public class AssessmentManager : IAssessmentManager
{
    private readonly TaskverseContext _context;
    private readonly AssessmentSettings _assessmentSettings;

    public AssessmentManager(
        TaskverseContext context,
        IOptions<AssessmentSettings> assessmentSettings)
    {
        _context = context;
        _assessmentSettings = assessmentSettings.Value;
    }

    public async Task<Assessment> CreateAssessment(Assessment assessment, List<Guid> questionIds)
    {
        ValidateAssessment(assessment, questionIds);

        var classification = await SubjectTopicResolver.ResolveAsync(
            _context,
            assessment.SubjectId,
            assessment.SubjectName,
            assessment.TopicId,
            assessment.TopicName);

        assessment.SubjectId = classification.Subject.SubjectId;
        assessment.Subject = classification.Subject;
        assessment.SubjectName = classification.Subject.SubjectName;
        assessment.TopicId = classification.Topic.TopicId;
        assessment.Topic = classification.Topic;
        assessment.TopicName = classification.Topic.TopicName;

        var normalizedQuestionIds = questionIds
            .Where(questionId => questionId != Guid.Empty)
            .Distinct()
            .ToList();

        var normalizedAssignedBatchIds = assessment.AssignedBatchIds
            .Where(batchId => batchId != Guid.Empty)
            .Distinct()
            .ToArray();

        if (normalizedAssignedBatchIds.Length > 0)
        {
            var validBatchIds = await _context.Batches
                .AsNoTracking()
                .Where(batch => batch.CollegeId == assessment.CollegeId && normalizedAssignedBatchIds.Contains(batch.BatchId))
                .Select(batch => batch.BatchId)
                .ToListAsync();

            var invalidBatchIds = normalizedAssignedBatchIds.Except(validBatchIds).ToArray();
            if (invalidBatchIds.Length > 0)
            {
                throw new InvalidOperationException(
                    $"Batch id(s) do not belong to this college or were not found: {string.Join(", ", invalidBatchIds)}.");
            }
        }

        assessment.AssignedBatchIds = normalizedAssignedBatchIds;

        if (normalizedQuestionIds.Count == 0)
        {
            throw new ArgumentException("At least one valid question id is required.");
        }

        var questions = await _context.Questions
            .Where(question => normalizedQuestionIds.Contains(question.QuestionId))
            .ToListAsync();

        var missingQuestionIds = normalizedQuestionIds.Except(questions.Select(question => question.QuestionId)).ToList();
        if (missingQuestionIds.Count > 0)
        {
            throw new KeyNotFoundException($"Question(s) not found: {string.Join(", ", missingQuestionIds)}.");
        }

        var wrongCollegeQuestionIds = questions
            .Where(question => question.CollegeId != assessment.CollegeId)
            .Select(question => question.QuestionId)
            .ToList();
        if (wrongCollegeQuestionIds.Count > 0)
        {
            throw new InvalidOperationException($"Question(s) do not belong to this college: {string.Join(", ", wrongCollegeQuestionIds)}.");
        }

        var unavailableQuestionIds = questions
            .Where(question => !question.IsActive || question.AssessmentId.HasValue)
            .Select(question => question.QuestionId)
            .ToList();
        if (unavailableQuestionIds.Count > 0)
        {
            throw new InvalidOperationException($"Question(s) are not available in the question bank: {string.Join(", ", unavailableQuestionIds)}.");
        }

        var subjectMismatchedQuestionIds = questions
            .Where(question => !string.Equals(
                question.Subject?.Trim(),
                classification.Subject.SubjectName,
                StringComparison.OrdinalIgnoreCase))
            .Select(question => question.QuestionId)
            .ToList();
        if (subjectMismatchedQuestionIds.Count > 0)
        {
            throw new InvalidOperationException(
                $"Question(s) do not belong to subject '{classification.Subject.SubjectName}': {string.Join(", ", subjectMismatchedQuestionIds)}.");
        }

        var topicMismatchedQuestionIds = questions
            .Where(question => !string.Equals(
                question.Topic?.Trim(),
                classification.Topic.TopicName,
                StringComparison.OrdinalIgnoreCase))
            .Select(question => question.QuestionId)
            .ToList();
        if (topicMismatchedQuestionIds.Count > 0)
        {
            throw new InvalidOperationException(
                $"Question(s) do not belong to topic '{classification.Topic.TopicName}': {string.Join(", ", topicMismatchedQuestionIds)}.");
        }

        ValidateQuestionBudget(assessment, questions);

        assessment.AssessmentId = assessment.AssessmentId == Guid.Empty ? Guid.NewGuid() : assessment.AssessmentId;
        assessment.AssessmentType = ResolveAssessmentType(questions);
        assessment.DifficultyLevel = CalculateDifficultyLevel(questions);
        assessment.CreatedAt = DateTime.UtcNow;

        var questionOrder = normalizedQuestionIds
            .Select((questionId, index) => new { questionId, displayOrder = index + 1 })
            .ToDictionary(item => item.questionId, item => item.displayOrder);

        assessment.AssessmentQuestions = questions
            .OrderBy(question => questionOrder[question.QuestionId])
            .Select(question => new AssessmentQuestion
            {
                AssessmentId = assessment.AssessmentId,
                QuestionId = question.QuestionId,
                DisplayOrder = questionOrder[question.QuestionId],
                Marks = question.Marks
            })
            .ToList();

        foreach (var question in questions)
        {
            question.AssessmentId = assessment.AssessmentId;
            question.ModifiedAt = DateTime.UtcNow;
        }

        _context.Assessments.Add(assessment);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Unable to save the assessment.", ex);
        }

        return assessment;
    }

    private static void ValidateAssessment(Assessment assessment, List<Guid> questionIds)
    {
        if (assessment.CollegeId == Guid.Empty)
        {
            throw new ArgumentException("CollegeId is required.");
        }

        if (string.IsNullOrWhiteSpace(assessment.CreatedBy))
        {
            throw new ArgumentException("CreatedBy is required.");
        }

        if (string.IsNullOrWhiteSpace(assessment.AssessmentName))
        {
            throw new ArgumentException("Assessment name is required.");
        }

        if (assessment.DurationMinutes <= 0)
        {
            throw new ArgumentException("Duration minutes must be greater than zero.");
        }

        if (assessment.TotalMarks < 0)
        {
            throw new ArgumentException("Total marks cannot be negative.");
        }

        if (assessment.EndDateTime.HasValue &&
            assessment.StartDateTime.HasValue &&
            assessment.EndDateTime <= assessment.StartDateTime)
        {
            throw new ArgumentException("End datetime must be greater than start datetime.");
        }

        if (questionIds.Count == 0)
        {
            throw new ArgumentException("At least one question id is required.");
        }
    }

    private void ValidateQuestionBudget(Assessment assessment, List<Question> questions)
    {
        var selectedQuestionCount = questions.Count;
        var selectedMarks = questions.Sum(question => question.Marks);
        var allowedByMarks = CalculateAllowedQuestionCountByMarks(assessment.TotalMarks);

        if (allowedByMarks.HasValue && selectedQuestionCount > allowedByMarks.Value)
        {
            throw new AssessmentQuestionLimitException(
                $"Selected question count ({selectedQuestionCount}) exceeds the limit allowed by total marks ({allowedByMarks.Value}).");
        }

        if (selectedMarks > assessment.TotalMarks)
        {
            throw new AssessmentQuestionLimitException(
                $"Selected question marks ({selectedMarks}) exceed assessment total marks ({assessment.TotalMarks}).");
        }

        var codingQuestionCount = questions.Count(IsCodingQuestion);
        var nonCodingQuestionCount = selectedQuestionCount - codingQuestionCount;
        var requiredDurationMinutes =
            codingQuestionCount * _assessmentSettings.CodingTimePerQuestionMinutes +
            nonCodingQuestionCount * _assessmentSettings.NonCodingTimePerQuestionMinutes;

        if (requiredDurationMinutes > assessment.DurationMinutes)
        {
            var codingLimit = CalculateAllowedQuestionCountByDuration(
                assessment.DurationMinutes,
                _assessmentSettings.CodingTimePerQuestionMinutes);
            var nonCodingLimit = CalculateAllowedQuestionCountByDuration(
                assessment.DurationMinutes,
                _assessmentSettings.NonCodingTimePerQuestionMinutes);

            throw new AssessmentQuestionLimitException(
                $"Selected questions require {requiredDurationMinutes} minutes, but assessment duration is {assessment.DurationMinutes} minutes. " +
                $"Allowed by duration: {codingLimit} coding question(s) or {nonCodingLimit} non-coding question(s).");
        }
    }

    private int? CalculateAllowedQuestionCountByMarks(int totalMarks)
    {
        if (totalMarks <= 0 || _assessmentSettings.MarksPerQuestion <= 0)
        {
            return null;
        }

        return (int)Math.Floor(totalMarks / _assessmentSettings.MarksPerQuestion);
    }

    private static int CalculateAllowedQuestionCountByDuration(int durationMinutes, decimal timePerQuestionMinutes)
    {
        if (durationMinutes <= 0 || timePerQuestionMinutes <= 0)
        {
            return 0;
        }

        return (int)Math.Floor(durationMinutes / timePerQuestionMinutes);
    }

    private static AssessmentType ResolveAssessmentType(IEnumerable<Question> questions)
    {
        var normalizedTypes = questions
            .Select(question => question.QuestionType.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        if (normalizedTypes.All(type => type == "coding"))
        {
            return AssessmentType.Coding;
        }

        return normalizedTypes.Any(type => type == "coding")
            ? AssessmentType.Mixed
            : AssessmentType.Objective;
    }

    private static bool IsCodingQuestion(Question question)
        => string.Equals(question.QuestionType?.Trim(), "coding", StringComparison.OrdinalIgnoreCase);

    private static int CalculateDifficultyLevel(IEnumerable<Question> questions)
    {
        var roundedAverage = (int)Math.Round(questions.Average(question => question.DifficultyLevel), MidpointRounding.AwayFromZero);
        return Math.Max(1, roundedAverage);
    }
}
