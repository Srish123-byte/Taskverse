using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;
using Taskverse.API.Assessments.Service.Mappings;
using Taskverse.Data.DataAccess;
using Taskverse.Data.Enums;

namespace Taskverse.API.Assessments.Service.Managers;

public class QuestionManager : IQuestionManager
{
    private static readonly Regex FillInTheBlankPlaceholderPattern = new("_{3,}", RegexOptions.Compiled);
    private static readonly HashSet<string> AllowedQuestionTypes =
    [
        "mcq",
        "fill in the blanks"
    ];

    private readonly TaskverseContext _context;

    public QuestionManager(TaskverseContext context)
    {
        _context = context;
    }

    public async Task<List<Question>> CreateQuestions(List<QuestionImportItem> questions)
    {
        if (questions.Count == 0)
        {
            throw new ArgumentException("At least one question is required.");
        }

        var preparedQuestions = new List<Question>();
        var importFingerprints = new HashSet<string>(StringComparer.Ordinal);

        foreach (var importItem in questions)
        {
            var question = importItem.Question;

            try
            {
                await NormalizeSubjectTopicAsync(question);
                ValidateQuestion(question);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
            {
                throw new ArgumentException($"Row {importItem.SourceRowNumber}: {ex.Message}");
            }

            question.QuestionId = question.QuestionId == Guid.Empty ? Guid.NewGuid() : question.QuestionId;
            question.IsActive = true;
            question.CreatedAt = DateTime.UtcNow;
            question.ModifiedAt = DateTime.UtcNow;
            question.Version = question.Version <= 0 ? 1 : question.Version;

            var fingerprint = BuildDuplicateFingerprint(question);
            if (!importFingerprints.Add(fingerprint))
            {
                continue;
            }

            preparedQuestions.Add(question);
        }

        if (preparedQuestions.Count == 0)
        {
            return [];
        }

        var normalizedQuestionTexts = preparedQuestions
            .Select(question => NormalizeForLookup(question.QuestionText))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct()
            .ToList();

        var collegeIds = preparedQuestions
            .Select(question => question.CollegeId)
            .Distinct()
            .ToList();

        var existingQuestions = await _context.Questions
            .AsNoTracking()
            .Where(question =>
                question.IsActive &&
                collegeIds.Contains(question.CollegeId) &&
                normalizedQuestionTexts.Contains(question.QuestionText.ToLower()))
            .ToListAsync();

        var existingFingerprints = existingQuestions
            .Select(BuildDuplicateFingerprint)
            .ToHashSet(StringComparer.Ordinal);

        var uniqueQuestionsToCreate = preparedQuestions
            .Where(question => !existingFingerprints.Contains(BuildDuplicateFingerprint(question)))
            .ToList();

        if (uniqueQuestionsToCreate.Count == 0)
        {
            return [];
        }

        _context.Questions.AddRange(uniqueQuestionsToCreate);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Unable to save the question to the question bank.", ex);
        }

        return uniqueQuestionsToCreate;
    }

    public async Task<Question> GetQuestionById(Guid collegeId, Guid questionId)
    {
        if (collegeId == Guid.Empty)
        {
            throw new ArgumentException("CollegeId is required.");
        }

        if (questionId == Guid.Empty)
        {
            throw new ArgumentException("QuestionId is required.");
        }

        var question = await _context.Questions
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.QuestionId == questionId &&
                item.CollegeId == collegeId &&
                item.IsActive);

        if (question is null)
        {
            throw new KeyNotFoundException($"Question with id '{questionId}' was not found.");
        }

        await EnsureQuestionIsNotInLiveAssessmentAsync(question.QuestionId, question.AssessmentId);
        await SubjectTopicResolver.PopulateQuestionSubjectTopicIdsAsync(_context, [question]);

        return question;
    }

    public async Task<Question> UpdateQuestion(Guid questionId, Question updatedQuestion, string? requesterRole)
    {
        await NormalizeSubjectTopicAsync(updatedQuestion);
        ValidateQuestion(updatedQuestion);

        var existingQuestion = await _context.Questions.FirstOrDefaultAsync(question => question.QuestionId == questionId);
        if (existingQuestion is null)
        {
            throw new KeyNotFoundException($"Question with id '{questionId}' was not found.");
        }

        await EnsureQuestionIsNotInLiveAssessmentAsync(existingQuestion.QuestionId, existingQuestion.AssessmentId);

        if (IsTrainer(requesterRole) &&
            !string.Equals(existingQuestion.CreatedBy?.Trim(), updatedQuestion.CreatedBy?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Only the user who created this question can update it.");
        }

        existingQuestion.ApplyUpdates(updatedQuestion);
        existingQuestion.Version += 1;
        existingQuestion.ModifiedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Unable to update the question in the question bank.", ex);
        }

        return existingQuestion;
    }

    private static bool IsTrainer(string? requesterRole)
    {
        return string.Equals(requesterRole?.Trim(), "Trainer", StringComparison.OrdinalIgnoreCase);
    }

    private async Task EnsureQuestionIsNotInLiveAssessmentAsync(Guid questionId, Guid? assessmentId)
    {
        if (assessmentId.HasValue && assessmentId.Value != Guid.Empty)
        {
            var linkedAssessment = await _context.Assessments
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.AssessmentId == assessmentId.Value);

            if (linkedAssessment?.AssessmentStatus == AssessmentStatus.Live)
            {
                throw new InvalidOperationException("This question cannot be edited because it is included in a live assessment.");
            }
        }

        var liveAssessmentLinkExists = await _context.AssessmentQuestions
            .AsNoTracking()
            .Join(
                _context.Assessments.AsNoTracking(),
                assessmentQuestion => assessmentQuestion.AssessmentId,
                assessment => assessment.AssessmentId,
                (assessmentQuestion, assessment) => new
                {
                    assessmentQuestion.QuestionId,
                    assessment.AssessmentStatus
                })
            .AnyAsync(item =>
                item.QuestionId == questionId &&
                item.AssessmentStatus == AssessmentStatus.Live);

        if (liveAssessmentLinkExists)
        {
            throw new InvalidOperationException("This question cannot be edited because it is included in a live assessment.");
        }
    }

    public async Task<List<Guid>> DeleteQuestions(
        string createdBy,
        string? requesterRole,
        Guid collegeId,
        List<Guid> questionIds)
    {
        if (string.IsNullOrWhiteSpace(createdBy))
        {
            throw new ArgumentException("CreatedBy is required.");
        }

        if (collegeId == Guid.Empty)
        {
            throw new ArgumentException("CollegeId is required.");
        }

        var normalizedQuestionIds = questionIds.NormalizeQuestionIds();
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

        var outOfCollegeQuestion = questions.FirstOrDefault(question => question.CollegeId != collegeId);
        if (outOfCollegeQuestion is not null)
        {
            throw new UnauthorizedAccessException("You are not authorized to delete questions outside your college question bank.");
        }

        var unauthorizedQuestion = questions.FirstOrDefault(question =>
            IsTrainer(requesterRole) &&
            !string.Equals(question.CreatedBy?.Trim(), createdBy.Trim(), StringComparison.OrdinalIgnoreCase));
        if (unauthorizedQuestion is not null)
        {
            throw new UnauthorizedAccessException("You're not authorized to delete this question. Please try deleting a question you've created");
        }

        var linkedAssessmentStatuses = await GetLinkedAssessmentStatusesAsync(questions);

        if (linkedAssessmentStatuses.Any(status => status == AssessmentStatus.Scheduled))
        {
            throw new InvalidOperationException("Delete the question from the scheduled assessment(s) and try again.");
        }

        if (linkedAssessmentStatuses.Any(status => status is AssessmentStatus.Live or AssessmentStatus.Completed))
        {
            throw new InvalidOperationException("Deleting a question in the Live/Completed assessment(s) isn't allowed");
        }

        var linkedAssessmentQuestions = await _context.AssessmentQuestions
            .Where(item => normalizedQuestionIds.Contains(item.QuestionId))
            .ToListAsync();

        if (linkedAssessmentQuestions.Count > 0)
        {
            _context.AssessmentQuestions.RemoveRange(linkedAssessmentQuestions);
        }

        _context.Questions.RemoveRange(questions);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Unable to delete the question(s) from the question bank.", ex);
        }

        return questions.Select(question => question.QuestionId).ToList();
    }

    private async Task<HashSet<AssessmentStatus>> GetLinkedAssessmentStatusesAsync(IEnumerable<Question> questions)
    {
        var statusSet = new HashSet<AssessmentStatus>();
        var questionList = questions.ToList();
        var questionIds = questionList.Select(question => question.QuestionId).ToList();

        var directAssessmentIds = questionList
            .Where(question => question.AssessmentId.HasValue && question.AssessmentId.Value != Guid.Empty)
            .Select(question => question.AssessmentId!.Value)
            .Distinct()
            .ToList();

        if (directAssessmentIds.Count > 0)
        {
            var directStatuses = await _context.Assessments
                .AsNoTracking()
                .Where(item => directAssessmentIds.Contains(item.AssessmentId))
                .Select(item => item.AssessmentStatus)
                .ToListAsync();

            foreach (var status in directStatuses)
            {
                statusSet.Add(status);
            }
        }

        var linkedStatuses = await _context.AssessmentQuestions
            .AsNoTracking()
            .Join(
                _context.Assessments.AsNoTracking(),
                assessmentQuestion => assessmentQuestion.AssessmentId,
                assessment => assessment.AssessmentId,
                (assessmentQuestion, assessment) => new
                {
                    assessmentQuestion.QuestionId,
                    assessment.AssessmentStatus
                })
            .Where(item => questionIds.Contains(item.QuestionId))
            .Select(item => item.AssessmentStatus)
            .Distinct()
            .ToListAsync();

        foreach (var status in linkedStatuses)
        {
            statusSet.Add(status);
        }

        return statusSet;
    }

    public async Task<(List<Question> Items, int TotalCount)> SearchQuestionBank(
        Guid collegeId,
        int? difficultyLevel,
        Guid? subjectId,
        Guid? topicId,
        string? subject,
        string? topic,
        int pageNumber,
        int pageSize)
    {
        if (collegeId == Guid.Empty)
        {
            throw new ArgumentException("CollegeId is required.");
        }

        pageNumber = pageNumber > 0 ? pageNumber : 1;
        pageSize = pageSize is > 0 and <= 100 ? pageSize : 10;

        var query = _context.Questions
            .AsNoTracking()
            .Where(question =>
                question.CollegeId == collegeId &&
                question.IsActive);

        if (subjectId.HasValue && subjectId.Value != Guid.Empty)
        {
            var resolvedSubject = await _context.Subjects
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.SubjectId == subjectId.Value && item.IsActive);

            if (resolvedSubject is null)
            {
                throw new KeyNotFoundException($"Subject with id '{subjectId}' was not found.");
            }

            subject = resolvedSubject.SubjectName;
        }

        if (topicId.HasValue && topicId.Value != Guid.Empty)
        {
            var resolvedTopic = await _context.Topics
                .AsNoTracking()
                .Include(item => item.Subject)
                .FirstOrDefaultAsync(item => item.TopicId == topicId.Value && item.IsActive);

            if (resolvedTopic is null)
            {
                throw new KeyNotFoundException($"Topic with id '{topicId}' was not found.");
            }

            if (subjectId.HasValue && subjectId.Value != Guid.Empty && resolvedTopic.SubjectId != subjectId.Value)
            {
                throw new InvalidOperationException("Topic does not belong to the specified subject.");
            }

            topic = resolvedTopic.TopicName;
            subject ??= resolvedTopic.Subject.SubjectName;
        }

        if (difficultyLevel.HasValue)
        {
            query = query.Where(question => question.DifficultyLevel == difficultyLevel.Value);
        }

        if (!string.IsNullOrWhiteSpace(subject))
        {
            var normalizedSubject = subject.Trim().ToLower();
            query = query.Where(question => question.Subject != null && question.Subject.ToLower() == normalizedSubject);
        }

        if (!string.IsNullOrWhiteSpace(topic))
        {
            var normalizedTopic = topic.Trim().ToLower();
            query = query.Where(question => question.Topic != null && question.Topic.ToLower() == normalizedTopic);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(question => question.CreatedAt)
            .ThenBy(question => question.Subject)
            .ThenBy(question => question.Topic)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        await SubjectTopicResolver.PopulateQuestionSubjectTopicIdsAsync(_context, items);

        return (items, totalCount);
    }

    private async Task NormalizeSubjectTopicAsync(Question question)
    {
        var classification = await SubjectTopicResolver.ResolveAsync(
            _context,
            question.SubjectId,
            question.Subject,
            question.TopicId,
            question.Topic);

        question.SubjectId = classification.Subject.SubjectId;
        question.Subject = classification.Subject.SubjectName;
        question.TopicId = classification.Topic.TopicId;
        question.Topic = classification.Topic.TopicName;
    }

    private static void ValidateQuestion(Question question)
    {
        if (question.CollegeId == Guid.Empty)
        {
            throw new ArgumentException("CollegeId is required.");
        }

        if (string.IsNullOrWhiteSpace(question.CreatedBy))
        {
            throw new ArgumentException("CreatedBy is required.");
        }

        if (string.IsNullOrWhiteSpace(question.Stream))
        {
            throw new ArgumentException("Stream is required.");
        }

        if (string.IsNullOrWhiteSpace(question.Subject))
        {
            throw new ArgumentException("Subject is required.");
        }

        if (string.IsNullOrWhiteSpace(question.Topic))
        {
            throw new ArgumentException("Topic is required.");
        }

        if (question.TopicTag is null || question.TopicTag.Length == 0 || question.TopicTag.All(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Topic tag is required.");
        }

        if (string.IsNullOrWhiteSpace(question.QuestionType))
        {
            throw new ArgumentException("Question type is required.");
        }

        var normalizedQuestionType = question.QuestionType.Trim().ToLowerInvariant();
        if (!AllowedQuestionTypes.Contains(normalizedQuestionType))
        {
            throw new ArgumentException("Question type must be either 'mcq' or 'fill in the blanks'.");
        }

        question.QuestionType = normalizedQuestionType;

        if (string.IsNullOrWhiteSpace(question.QuestionText))
        {
            throw new ArgumentException("Question text is required.");
        }

        if (normalizedQuestionType == "mcq" && !HasMinimumValidOptions(question.Options, 4))
        {
            throw new ArgumentException("Options are required for mcq questions.");
        }

        if (normalizedQuestionType == "fill in the blanks")
        {
            if (!FillInTheBlankPlaceholderPattern.IsMatch(question.QuestionText))
            {
                throw new ArgumentException("Fill in the blanks questions must include a blank shown with underscore characters like ____ in the question text.");
            }

            if (!HasMinimumValidOptions(question.Options, 4))
            {
                throw new ArgumentException("Four options are required for fill in the blanks questions.");
            }
        }

        if (string.IsNullOrWhiteSpace(question.Answer))
        {
            throw new ArgumentException("Answer is required.");
        }

        if (question.Marks < 0)
        {
            throw new ArgumentException("Marks cannot be negative.");
        }

        if (question.NegativeMarks < 0)
        {
            throw new ArgumentException("Negative marks cannot be negative.");
        }
    }

    private static bool HasMinimumValidOptions(string? options, int minimumCount)
    {
        if (string.IsNullOrWhiteSpace(options))
        {
            return false;
        }

        try
        {
            var parsedOptions = JsonSerializer.Deserialize<List<string>>(options);
            return parsedOptions is not null &&
                   parsedOptions.Count >= minimumCount &&
                   parsedOptions.All(option => !string.IsNullOrWhiteSpace(option));
        }
        catch
        {
            return false;
        }
    }

    private static string BuildDuplicateFingerprint(Question question)
    {
        var normalizedOptions = NormalizeOptions(question.Options);

        return string.Join("|", [
            question.CollegeId.ToString("D"),
            NormalizeForLookup(question.Stream),
            NormalizeForLookup(question.Subject),
            NormalizeForLookup(question.Topic),
            NormalizeTopicTagsForLookup(question.TopicTag),
            NormalizeForLookup(question.QuestionType),
            NormalizeForLookup(question.QuestionText),
            normalizedOptions,
            NormalizeForLookup(question.Answer),
            NormalizeForLookup(question.Explanation),
            question.Marks.ToString("0.##"),
            question.NegativeMarks.ToString("0.##"),
            question.DifficultyLevel.ToString()
        ]);
    }

    private static string NormalizeOptions(string? options)
    {
        if (string.IsNullOrWhiteSpace(options))
        {
            return string.Empty;
        }

        try
        {
            var parsedOptions = JsonSerializer.Deserialize<List<string>>(options) ?? [];
            return string.Join("~", parsedOptions.Select(NormalizeForLookup));
        }
        catch
        {
            return NormalizeForLookup(options);
        }
    }

    private static string NormalizeForLookup(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Join(" ", value.Trim().ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string NormalizeTopicTagsForLookup(IEnumerable<string>? values)
    {
        return string.Join(
            "~",
            (values ?? [])
                .Select(NormalizeForLookup)
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal));
    }
}
