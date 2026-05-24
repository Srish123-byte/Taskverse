using System.Text.Json;
using Taskverse.API.Assessments.Service.Models;
using Taskverse.Business.Enums;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.Assessments.Service.Mappings;

public static class AssessmentMappings
{
    public static Assessment ToEntity(this CreateAssessmentRequest request, AssessmentSettings settings)
    {
        return new Assessment
        {
            CollegeId = request.CollegeId,
            SubjectId = request.SubjectId,
            SubjectName = request.SubjectName?.Trim(),
            TopicId = request.TopicId,
            TopicName = request.TopicName?.Trim(),
            AssessmentName = request.AssessmentName.Trim(),
            AssessmentStatus = AssessmentStatus.Draft,
            DurationMinutes = request.DurationMinutes,
            TotalMarks = request.TotalMarks,
            StartDateTime = request.StartDateTime,
            EndDateTime = request.EndDateTime,
            Instructions = settings.Instructions,
            AssignedBatchIds = request.AssignedBatchIds
                .Where(batchId => batchId != Guid.Empty)
                .Distinct()
                .ToArray(),
            AllowLateEntry = settings.IsLateEntryAllowed,
            ShowResultsImmediately = settings.IsResultsAvailableImmediately,
            AllowQuestionReview = settings.AllowQuestionReview,
            NegativeMarking = settings.NegativeMarking,
            MarksPerQuestion = settings.MarksPerQuestion,
            IsTotalMarksAutoCalculated = settings.IsTotalMarksAutoCalculated,
            CreatedBy = request.CreatedBy
        };
    }

    public static AssessmentRecord ToRecord(this Assessment assessment)
    {
        return new AssessmentRecord(
            assessment.AssessmentId,
            assessment.CollegeId,
            assessment.SubjectId,
            assessment.Subject?.SubjectName ?? assessment.SubjectName,
            assessment.TopicId,
            assessment.Topic?.TopicName ?? assessment.TopicName,
            assessment.AssessmentName,
            ToApiAssessmentType(assessment.AssessmentType),
            assessment.AssessmentStatus.ToString().ToLowerInvariant(),
            assessment.DurationMinutes,
            assessment.TotalMarks,
            assessment.DifficultyLevel,
            assessment.StartDateTime,
            assessment.EndDateTime,
            assessment.Instructions,
            assessment.AssignedBatchIds,
            assessment.AllowLateEntry,
            assessment.ShowResultsImmediately,
            assessment.AllowQuestionReview,
            assessment.NegativeMarking,
            assessment.MarksPerQuestion,
            assessment.IsTotalMarksAutoCalculated,
            assessment.CreatedBy,
            assessment.CreatedAt,
            assessment.ModifiedAt,
            assessment.AssessmentQuestions
                .OrderBy(question => question.DisplayOrder)
                .Select(question => question.QuestionId)
                .ToList());
    }

    private static string ToApiAssessmentType(AssessmentType assessmentType)
    {
        return assessmentType switch
        {
            AssessmentType.Coding => "coding",
            AssessmentType.Mixed => "mixed",
            _ => "mcq"
        };
    }

    public static AssessmentQuestionListItemRecord ToQuestionListItemRecord(
        this Question question,
        int displayOrder)
    {
        return new AssessmentQuestionListItemRecord(
            question.QuestionId,
            displayOrder,
            question.QuestionType,
            question.QuestionText,
            DeserializeOptions(question.Options),
            question.Marks,
            question.NegativeMarks,
            question.DifficultyLevel);
    }

    private static List<string>? DeserializeOptions(string? options)
    {
        if (string.IsNullOrWhiteSpace(options))
        {
            return null;
        }

        return JsonSerializer.Deserialize<List<string>>(options);
    }
}
