using System.Text.Json;
using Taskverse.API.Assessments.Service.Models;
using Taskverse.Data.Enums;
using Taskverse.Data.DataAccess;
using Taskverse.Data.Utilities;

namespace Taskverse.API.Assessments.Service.Mappings;

public static class AssessmentMappings
{
    public static Assessment ToEntity(
        this CreateAssessmentRequest request,
        AssessmentSettings settings,
        AssessmentStatus assessmentStatus = AssessmentStatus.Draft)
    {
        var isDraft = assessmentStatus == AssessmentStatus.Draft;

        return new Assessment
        {
            CollegeId = request.CollegeId,
            SubjectId = request.SubjectId,
            SubjectName = request.SubjectName?.Trim(),
            TopicId = request.TopicId,
            TopicName = request.TopicName?.Trim(),
            AssessmentName = NormalizeAssessmentName(request.AssessmentName, isDraft),
            AssessmentStatus = assessmentStatus,
            DurationMinutes = NormalizeDurationMinutes(request.DurationMinutes, isDraft),
            TotalMarks = NormalizeTotalMarks(request.TotalMarks),
            StartDateTime = UtcDateTime.Normalize(request.StartDateTime),
            EndDateTime = UtcDateTime.Normalize(request.EndDateTime),
            Instructions = string.IsNullOrWhiteSpace(request.Instructions) ? null : request.Instructions.Trim(),
            AssignedBatchIds = (request.AssignedBatchIds ?? [])
                .Where(batchId => batchId != Guid.Empty)
                .Distinct()
                .ToArray(),
            AllowLateEntry = request.AllowLateEntry,
            ShowResultsImmediately = settings.IsResultsAvailableImmediately,
            PassingPercentage = request.PassingPercentage,
            AllowQuestionReview = request.AllowQuestionReview,
            NegativeMarking = request.NegativeMarking,
            IsTotalMarksAutoCalculated = settings.IsTotalMarksAutoCalculated,
            CreatedBy = request.CreatedBy
        };
    }

    public static CreateAssessmentRequest ToCreateAssessmentRequest(this PublishAssessmentRequest request)
    {
        return new CreateAssessmentRequest
        {
            CollegeId = request.CollegeId,
            CreatedBy = request.CreatedBy,
            AssessmentName = request.AssessmentName,
            SubjectId = request.SubjectId,
            SubjectName = request.SubjectName,
            TopicId = request.TopicId,
            TopicName = request.TopicName,
            Instructions = request.Instructions,
            AllowLateEntry = request.AllowLateEntry,
            AllowQuestionReview = request.AllowQuestionReview,
            NegativeMarking = request.NegativeMarking,
            PassingPercentage = request.PassingPercentage,
            AssignedBatchIds = request.AssignedBatchIds,
            QuestionIds = request.QuestionIds,
            DurationMinutes = request.DurationMinutes,
            TotalMarks = request.TotalMarks,
            StartDateTime = request.StartDateTime,
            EndDateTime = request.EndDateTime
        };
    }

    public static Assessment ToEntity(
        this UpdateAssessmentRequest request,
        AssessmentSettings settings,
        AssessmentStatus assessmentStatus)
    {
        return new Assessment
        {
            AssessmentId = request.AssessmentId,
            CollegeId = request.CollegeId,
            SubjectId = request.SubjectId,
            SubjectName = request.SubjectName?.Trim(),
            TopicId = request.TopicId,
            TopicName = request.TopicName?.Trim(),
            AssessmentName = NormalizeAssessmentName(request.AssessmentName, assessmentStatus == AssessmentStatus.Draft),
            AssessmentStatus = assessmentStatus,
            DurationMinutes = NormalizeDurationMinutes(request.DurationMinutes, assessmentStatus == AssessmentStatus.Draft),
            TotalMarks = NormalizeTotalMarks(request.TotalMarks),
            StartDateTime = UtcDateTime.Normalize(request.StartDateTime),
            EndDateTime = UtcDateTime.Normalize(request.EndDateTime),
            Instructions = string.IsNullOrWhiteSpace(request.Instructions) ? null : request.Instructions.Trim(),
            AssignedBatchIds = (request.AssignedBatchIds ?? [])
                .Where(batchId => batchId != Guid.Empty)
                .Distinct()
                .ToArray(),
            AllowLateEntry = request.AllowLateEntry,
            ShowResultsImmediately = settings.IsResultsAvailableImmediately,
            PassingPercentage = request.PassingPercentage,
            AllowQuestionReview = request.AllowQuestionReview,
            NegativeMarking = request.NegativeMarking,
            IsTotalMarksAutoCalculated = settings.IsTotalMarksAutoCalculated,
            CreatedBy = request.UpdatedBy
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
            UtcDateTime.Normalize(assessment.StartDateTime),
            UtcDateTime.Normalize(assessment.EndDateTime),
            assessment.Instructions,
            assessment.AssignedBatchIds,
            assessment.AllowLateEntry,
            assessment.ShowResultsImmediately,
            assessment.PassingPercentage,
            assessment.AllowQuestionReview,
            assessment.NegativeMarking,
            assessment.IsTotalMarksAutoCalculated,
            assessment.CreatedBy,
            UtcDateTime.Normalize(assessment.CreatedAt),
            UtcDateTime.Normalize(assessment.ModifiedAt),
            assessment.AssessmentQuestions
                .OrderBy(question => question.DisplayOrder)
                .Select(question => question.QuestionId)
                .ToList());
    }

    private static string NormalizeAssessmentName(string? assessmentName, bool allowDraftDefault)
    {
        var normalizedName = assessmentName?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedName))
        {
            return normalizedName;
        }

        return allowDraftDefault ? "Untitled draft" : string.Empty;
    }

    private static int NormalizeDurationMinutes(int durationMinutes, bool allowDraftDefault)
    {
        if (durationMinutes > 0)
        {
            return durationMinutes;
        }

        return allowDraftDefault ? 1 : durationMinutes;
    }

    private static int NormalizeTotalMarks(int totalMarks)
        => totalMarks < 0 ? 0 : totalMarks;

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

    public static StudentAssessmentListItemRecord ToStudentAssessmentListItemRecord(
        this Assessment assessment,
        string assessmentStatus)
    {
        return new StudentAssessmentListItemRecord(
            assessment.AssessmentId,
            assessment.AssessmentName,
            assessment.Subject?.SubjectName ?? assessment.SubjectName,
            assessment.Topic?.TopicName ?? assessment.TopicName,
            assessmentStatus,
            assessment.DurationMinutes,
            assessment.TotalMarks,
            assessment.DifficultyLevel,
            UtcDateTime.Normalize(assessment.StartDateTime),
            UtcDateTime.Normalize(assessment.EndDateTime));
    }

    public static StudentAssessmentDetailRecord ToStudentAssessmentDetailRecord(
        this Assessment assessment,
        int totalQuestions)
    {
        return new StudentAssessmentDetailRecord(
            assessment.AssessmentName,
            assessment.DurationMinutes,
            assessment.TotalMarks,
            totalQuestions,
            UtcDateTime.Normalize(assessment.StartDateTime),
            UtcDateTime.Normalize(assessment.EndDateTime),
            assessment.Instructions);
    }

    public static StudentAssessmentStartRecord ToStudentAssessmentStartRecord(this Attempt attempt)
    {
        return new StudentAssessmentStartRecord(
            attempt.AttemptId,
            attempt.AssessmentId,
            attempt.AttemptStatus.ToString().ToUpperInvariant(),
            UtcDateTime.Normalize(attempt.StartedAt));
    }

    public static StudentAttemptAnswerRecord ToStudentAttemptAnswerRecord(this AttemptAnswer attemptAnswer)
    {
        return new StudentAttemptAnswerRecord(
            attemptAnswer.QuestionId,
            attemptAnswer.SelectedAnswer,
            UtcDateTime.Normalize(attemptAnswer.AnsweredAt));
    }

    public static StudentAttemptSubmitRecord ToStudentAttemptSubmitRecord(this Attempt attempt)
    {
        return new StudentAttemptSubmitRecord(
            attempt.AttemptId,
            attempt.AttemptStatus.ToString().ToUpperInvariant(),
            UtcDateTime.Normalize(attempt.SubmittedAt));
    }

    public static StudentAttemptRecoveryRecord ToStudentAttemptRecoveryRecord(
        this Attempt attempt,
        Assessment assessment,
        int remainingSeconds,
        List<StudentAttemptRecoveryQuestionRecord> questions)
    {
        return new StudentAttemptRecoveryRecord(
            attempt.AttemptId,
            attempt.AssessmentId,
            assessment.AssessmentName,
            attempt.AttemptStatus.ToString().ToUpperInvariant(),
            UtcDateTime.Normalize(attempt.StartedAt),
            UtcDateTime.Normalize(attempt.SubmittedAt),
            UtcDateTime.Normalize(attempt.ExpiresAt),
            remainingSeconds,
            assessment.DurationMinutes,
            assessment.TotalMarks,
            attempt.TotalQuestions,
            attempt.AttemptedQuestions,
            attempt.UnansweredQuestions,
            assessment.Instructions,
            questions);
    }

    public static StudentAttemptRecoveryQuestionRecord ToStudentAttemptRecoveryQuestionRecord(
        this Question question,
        int displayOrder,
        AttemptAnswer? attemptAnswer)
    {
        return new StudentAttemptRecoveryQuestionRecord(
            question.QuestionId,
            displayOrder,
            question.QuestionType,
            question.QuestionText,
            DeserializeOptions(question.Options),
            question.Marks,
            question.NegativeMarks,
            question.DifficultyLevel,
            attemptAnswer?.SelectedAnswer,
            UtcDateTime.Normalize(attemptAnswer?.AnsweredAt));
    }

    private static List<string>? DeserializeOptions(string? options)
    {
        if (string.IsNullOrWhiteSpace(options))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
