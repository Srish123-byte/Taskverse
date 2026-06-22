using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;
using Taskverse.API.Assessments.Service.Mappings;
using Taskverse.API.Assessments.Service.Models;
using Taskverse.Data.DataAccess;
using Taskverse.Data.Enums;
using Taskverse.Data.Utilities;

namespace Taskverse.API.Assessments.Service.Managers;

/// <summary>
/// Handles validation, retrieval, update, delete, and search operations for question-bank entries.
/// </summary>
public class QuestionManager : IQuestionManager
{
    private const int DefaultCodingComparisonMode = 2;
    private static readonly Regex FillInTheBlankPlaceholderPattern = new("_{3,}", RegexOptions.Compiled);
    private static readonly HashSet<string> AllowedCodingInputFormats =
    [
        "stdin",
        "json",
        "function_args"
    ];
    private static readonly HashSet<string> AllowedQuestionTypes =
    [
        "mcq",
        "fill in the blanks",
        "coding"
    ];

    private readonly TaskverseContext _context;

    public QuestionManager(TaskverseContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<QuestionClassificationCatalogRecord> GetQuestionClassificationCatalog()
    {
        var subjects = await _context.Subjects
            .AsNoTracking()
            .Where(subject => subject.IsActive)
            .OrderBy(subject => subject.SubjectName)
            .ToListAsync();

        var topics = await _context.Topics
            .AsNoTracking()
            .Where(topic => topic.IsActive)
            .OrderBy(topic => topic.TopicName)
            .ToListAsync();

        var topicsBySubjectId = topics
            .GroupBy(topic => topic.SubjectId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(topic => topic.TopicName)
                    .Select(topic => topic.ToCatalogRecord())
                    .ToList());

        var subjectRecords = subjects
            .Select(subject => subject.ToCatalogRecord(
                topicsBySubjectId.TryGetValue(subject.SubjectId, out var subjectTopics)
                    ? subjectTopics
                    : []))
            .ToList();

        return new QuestionClassificationCatalogRecord(subjectRecords);
    }

    /// <inheritdoc />
    public async Task<List<QuestionRecord>> CreateQuestions(List<QuestionImportItem> questions)
    {
        if (questions.Count == 0)
        {
            throw new ArgumentException("At least one question is required.");
        }

        var createdRecordsByOrder = new Dictionary<int, QuestionRecord>();

        var standardItems = new List<QuestionImportItem>();
        var codingItems = new List<QuestionImportItem>();

        foreach (var question in questions)
        {
            var normalizedQuestionType = NormalizeQuestionType(question.Request.QuestionType);
            question.Request.QuestionType = normalizedQuestionType;

            if (normalizedQuestionType == "coding")
            {
                codingItems.Add(question);
            }
            else
            {
                standardItems.Add(question);
            }
        }

        foreach (var item in await CreateStandardQuestionsAsync(standardItems))
        {
            createdRecordsByOrder[item.InputOrder] = item.Record;
        }

        foreach (var item in await CreateCodingQuestionsAsync(codingItems))
        {
            createdRecordsByOrder[item.InputOrder] = item.Record;
        }

        return createdRecordsByOrder
            .OrderBy(item => item.Key)
            .Select(item => item.Value)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<QuestionRecord> GetQuestionById(Guid collegeId, Guid questionId)
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

        if (question is not null)
        {
            await EnsureQuestionIsNotInLiveAssessmentAsync(question.QuestionId);
            await SubjectTopicResolver.PopulateQuestionSubjectTopicIdsAsync(_context, [question]);
            return question.ToRecord();
        }

        var codingQuestion = await _context.CodingQuestions
            .AsNoTracking()
            .Include(item => item.TestCases)
            .FirstOrDefaultAsync(item =>
                item.CodingQuestionId == questionId &&
                item.CollegeId == collegeId &&
                item.IsActive);

        if (codingQuestion is null)
        {
            throw new KeyNotFoundException($"Question with id '{questionId}' was not found.");
        }

        await EnsureCodingQuestionIsNotInLiveAssessmentAsync(codingQuestion.CodingQuestionId);
        return codingQuestion.ToRecord();
    }

    /// <inheritdoc />
    public async Task<QuestionRecord> UpdateQuestion(Guid questionId, CreateQuestionRequest request, string? requesterRole)
    {
        var existingQuestion = await _context.Questions.FirstOrDefaultAsync(question => question.QuestionId == questionId);
        if (existingQuestion is not null)
        {
            var normalizedQuestionType = NormalizeQuestionType(request.QuestionType);
            if (normalizedQuestionType == "coding")
            {
                throw new InvalidOperationException("Existing non-coding questions cannot be converted to coding questions.");
            }

            var updatedQuestion = request.ToEntity();
            await NormalizeSubjectTopicAsync(updatedQuestion);
            ValidateQuestion(updatedQuestion);
            await EnsureQuestionIsNotInLiveAssessmentAsync(existingQuestion.QuestionId);

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

            return existingQuestion.ToRecord();
        }

        var existingCodingQuestion = await _context.CodingQuestions
            .Include(question => question.TestCases)
            .FirstOrDefaultAsync(question => question.CodingQuestionId == questionId);
        if (existingCodingQuestion is null)
        {
            throw new KeyNotFoundException($"Question with id '{questionId}' was not found.");
        }

        var codingQuestionType = NormalizeQuestionType(request.QuestionType);
        if (!string.IsNullOrWhiteSpace(codingQuestionType) && codingQuestionType != "coding")
        {
            throw new InvalidOperationException("Existing coding questions cannot be converted to a non-coding question type.");
        }

        if (request.NegativeMarks != 0)
        {
            throw new ArgumentException("Coding questions do not support negative marks.");
        }

        var updatedCodingQuestion = request.ToCodingEntity();
        var updatedTestCases = request.TestCases.ToTestCaseEntities();
        await ValidateCodingQuestionAsync(updatedCodingQuestion, updatedTestCases);
        await EnsureCodingQuestionIsNotInLiveAssessmentAsync(existingCodingQuestion.CodingQuestionId);

        if (IsTrainer(requesterRole) &&
            !string.Equals(existingCodingQuestion.CreatedBy?.Trim(), updatedCodingQuestion.CreatedBy?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Only the user who created this question can update it.");
        }

        existingCodingQuestion.ApplyUpdates(updatedCodingQuestion);
        existingCodingQuestion.Version += 1;
        existingCodingQuestion.ModifiedAt = DateTime.UtcNow;

        _context.TestCases.RemoveRange(existingCodingQuestion.TestCases);
        existingCodingQuestion.TestCases = updatedTestCases
            .Select(testCase =>
            {
                testCase.CodingQuestionId = existingCodingQuestion.CodingQuestionId;
                return testCase;
            })
            .ToList();

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Unable to update the coding question in the question bank.", ex);
        }

        return existingCodingQuestion.ToRecord();
    }

    private static bool IsTrainer(string? requesterRole)
    {
        return string.Equals(requesterRole?.Trim(), "Trainer", StringComparison.OrdinalIgnoreCase);
    }

    private async Task EnsureQuestionIsNotInLiveAssessmentAsync(Guid questionId)
    {
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

    /// <inheritdoc />
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
        var codingQuestions = await _context.CodingQuestions
            .Where(question => normalizedQuestionIds.Contains(question.CodingQuestionId))
            .ToListAsync();

        var foundQuestionIds = questions.Select(question => question.QuestionId)
            .Concat(codingQuestions.Select(question => question.CodingQuestionId))
            .ToHashSet();
        var missingQuestionIds = normalizedQuestionIds.Except(foundQuestionIds).ToList();
        if (missingQuestionIds.Count > 0)
        {
            throw new KeyNotFoundException($"Question(s) not found: {string.Join(", ", missingQuestionIds)}.");
        }

        var outOfCollegeQuestion = questions.FirstOrDefault(question => question.CollegeId != collegeId);
        var outOfCollegeCodingQuestion = codingQuestions.FirstOrDefault(question => question.CollegeId != collegeId);
        if (outOfCollegeQuestion is not null || outOfCollegeCodingQuestion is not null)
        {
            throw new UnauthorizedAccessException("You are not authorized to delete questions outside your college question bank.");
        }

        var unauthorizedQuestion = questions.FirstOrDefault(question =>
            IsTrainer(requesterRole) &&
            !string.Equals(question.CreatedBy?.Trim(), createdBy.Trim(), StringComparison.OrdinalIgnoreCase));
        var unauthorizedCodingQuestion = codingQuestions.FirstOrDefault(question =>
            IsTrainer(requesterRole) &&
            !string.Equals(question.CreatedBy?.Trim(), createdBy.Trim(), StringComparison.OrdinalIgnoreCase));
        if (unauthorizedQuestion is not null || unauthorizedCodingQuestion is not null)
        {
            throw new UnauthorizedAccessException("You're not authorized to delete this question. Please try deleting a question you've created");
        }

        var linkedAssessmentStatuses = await GetLinkedAssessmentStatusesAsync(questions, codingQuestions);

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

        var linkedAssessmentCodingQuestions = await _context.AssessmentCodingQuestions
            .Where(item => normalizedQuestionIds.Contains(item.CodingQuestionId))
            .ToListAsync();

        if (linkedAssessmentCodingQuestions.Count > 0)
        {
            _context.AssessmentCodingQuestions.RemoveRange(linkedAssessmentCodingQuestions);
        }

        _context.Questions.RemoveRange(questions);
        _context.CodingQuestions.RemoveRange(codingQuestions);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Unable to delete the question(s) from the question bank.", ex);
        }

        return questions.Select(question => question.QuestionId)
            .Concat(codingQuestions.Select(question => question.CodingQuestionId))
            .ToList();
    }

    private async Task<HashSet<AssessmentStatus>> GetLinkedAssessmentStatusesAsync(
        IEnumerable<Question> questions,
        IEnumerable<CodingQuestion> codingQuestions)
    {
        var statusSet = new HashSet<AssessmentStatus>();
        var questionList = questions.ToList();
        var questionIds = questionList.Select(question => question.QuestionId).ToList();
        var codingQuestionIds = codingQuestions.Select(question => question.CodingQuestionId).ToList();

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

        var linkedCodingStatuses = await _context.AssessmentCodingQuestions
            .AsNoTracking()
            .Join(
                _context.Assessments.AsNoTracking(),
                assessmentQuestion => assessmentQuestion.AssessmentId,
                assessment => assessment.AssessmentId,
                (assessmentQuestion, assessment) => new
                {
                    assessmentQuestion.CodingQuestionId,
                    assessment.AssessmentStatus
                })
            .Where(item => codingQuestionIds.Contains(item.CodingQuestionId))
            .Select(item => item.AssessmentStatus)
            .Distinct()
            .ToListAsync();

        foreach (var status in linkedCodingStatuses)
        {
            statusSet.Add(status);
        }

        return statusSet;
    }

    /// <inheritdoc />
    public async Task<(List<QuestionRecord> Items, int TotalCount)> SearchQuestionBank(
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

        var standardItems = await query
            .OrderByDescending(question => question.CreatedAt)
            .ThenBy(question => question.Subject)
            .ThenBy(question => question.Topic)
            .ToListAsync();

        await SubjectTopicResolver.PopulateQuestionSubjectTopicIdsAsync(_context, standardItems);

        var includeCodingQuestions = string.IsNullOrWhiteSpace(subject) &&
            string.IsNullOrWhiteSpace(topic) &&
            (!subjectId.HasValue || subjectId.Value == Guid.Empty) &&
            (!topicId.HasValue || topicId.Value == Guid.Empty);

        var codingItems = new List<CodingQuestion>();
        if (includeCodingQuestions)
        {
            var codingQuery = _context.CodingQuestions
                .AsNoTracking()
                .Include(question => question.TestCases)
                .Where(question =>
                    question.CollegeId == collegeId &&
                    question.IsActive);

            if (difficultyLevel.HasValue)
            {
                codingQuery = codingQuery.Where(question => question.DifficultyLevel == difficultyLevel.Value);
            }

            codingItems = await codingQuery
                .OrderByDescending(question => question.CreatedAt)
                .ThenBy(question => question.QuestionTitle)
                .ToListAsync();
        }

        var combinedItems = standardItems
            .Select(question => question.ToRecord())
            .Concat(codingItems.Select(question => question.ToRecord()))
            .OrderByDescending(question => question.CreatedAt)
            .ThenBy(question => question.Subject ?? question.QuestionTitle ?? string.Empty)
            .ThenBy(question => question.Topic ?? string.Empty)
            .ToList();

        var totalCount = combinedItems.Count;
        var pagedItems = combinedItems
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (pagedItems, totalCount);
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
            throw new ArgumentException("Question type must be either 'mcq', 'fill in the blanks', or 'coding'.");
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

        var normalizedOptions = DeserializeOptions(question.Options);
        var normalizedAnswers = QuestionAnswerJsonHelper.ParseStoredAnswers(question.Answer);
        if (normalizedAnswers.Count == 0)
        {
            throw new ArgumentException("Answer is required.");
        }

        question.Answer = QuestionAnswerJsonHelper.SerializeAnswers(normalizedAnswers);

        if (normalizedQuestionType == "mcq")
        {
            var optionLookup = new HashSet<string>(normalizedOptions ?? [], StringComparer.OrdinalIgnoreCase);
            if (normalizedAnswers.Any(answer => !optionLookup.Contains(answer)))
            {
                throw new ArgumentException("All answers must match one of the configured options.");
            }
        }

        if (normalizedQuestionType == "fill in the blanks" && normalizedAnswers.Count != 1)
        {
            throw new ArgumentException("Fill in the blanks questions support exactly one answer.");
        }

        if (normalizedQuestionType == "coding" && normalizedAnswers.Count != 1)
        {
            throw new ArgumentException("Coding questions require exactly one reference solution.");
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

    private async Task ValidateCodingQuestionAsync(CodingQuestion question, List<TestCase> testCases)
    {
        List<string> requestedLanguageCodes = string.IsNullOrWhiteSpace(question.DefaultLanguageCode)
            ? []
            : [question.DefaultLanguageCode.ToLowerInvariant()];

        var languageLookup = requestedLanguageCodes.Count == 0
            ? new Dictionary<string, CodingLanguage>(StringComparer.OrdinalIgnoreCase)
            : await _context.CodingLanguages
                .AsNoTracking()
                .Where(language => language.IsActive && requestedLanguageCodes.Contains(language.LanguageCode))
                .ToDictionaryAsync(language => language.LanguageCode, StringComparer.OrdinalIgnoreCase);

        var requestedComparisonModes = testCases
            .Select(testCase => testCase.ComparisonMode)
            .Distinct()
            .ToList();

        var comparisonModes = requestedComparisonModes.Count == 0
            ? new HashSet<short>()
            : await _context.LookupComparisonModes
                .AsNoTracking()
                .Where(mode => requestedComparisonModes.Contains(mode.ComparisonModeId))
                .Select(mode => mode.ComparisonModeId)
                .ToHashSetAsync();

        ValidateCodingQuestion(question, testCases, languageLookup, comparisonModes);
    }

    private async Task<List<(int InputOrder, QuestionRecord Record)>> CreateStandardQuestionsAsync(List<QuestionImportItem> items)
    {
        if (items.Count == 0)
        {
            return [];
        }

        var preparedQuestions = new List<(int InputOrder, Question Question)>();
        var importFingerprints = new HashSet<string>(StringComparer.Ordinal);

        foreach (var importItem in items)
        {
            var question = importItem.Request.ToEntity();

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

            preparedQuestions.Add((importItem.InputOrder, question));
        }

        if (preparedQuestions.Count == 0)
        {
            return [];
        }

        var normalizedQuestionTexts = preparedQuestions
            .Select(item => NormalizeForLookup(item.Question.QuestionText))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct()
            .ToList();

        var collegeIds = preparedQuestions
            .Select(item => item.Question.CollegeId)
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
            .Where(item => !existingFingerprints.Contains(BuildDuplicateFingerprint(item.Question)))
            .ToList();

        if (uniqueQuestionsToCreate.Count == 0)
        {
            return [];
        }

        _context.Questions.AddRange(uniqueQuestionsToCreate.Select(item => item.Question));

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Unable to save the question to the question bank.", ex);
        }

        return uniqueQuestionsToCreate
            .Select(item => (item.InputOrder, item.Question.ToRecord()))
            .ToList();
    }

    private async Task<List<(int InputOrder, QuestionRecord Record)>> CreateCodingQuestionsAsync(List<QuestionImportItem> items)
    {
        if (items.Count == 0)
        {
            return [];
        }

        var requestedLanguageCodes = items
            .Select(item => QuestionAnswerJsonHelper.NormalizeSingleValue(item.Request.DefaultLanguageCode)?.ToLowerInvariant())
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var languageLookup = requestedLanguageCodes.Count == 0
            ? new Dictionary<string, CodingLanguage>(StringComparer.OrdinalIgnoreCase)
            : await _context.CodingLanguages
                .AsNoTracking()
                .Where(language => language.IsActive && requestedLanguageCodes.Contains(language.LanguageCode))
                .ToDictionaryAsync(language => language.LanguageCode, StringComparer.OrdinalIgnoreCase);

        var requestedComparisonModes = items
            .SelectMany(item => item.Request.TestCases ?? [])
            .Select(testCase => testCase.ComparisonMode)
            .Distinct()
            .ToList();

        var comparisonModes = requestedComparisonModes.Count == 0
            ? new HashSet<short>()
            : await _context.LookupComparisonModes
                .AsNoTracking()
                .Where(mode => requestedComparisonModes.Contains(mode.ComparisonModeId))
                .Select(mode => mode.ComparisonModeId)
                .ToHashSetAsync();

        var preparedQuestions = new List<(int InputOrder, CodingQuestion Question)>();
        var importFingerprints = new HashSet<string>(StringComparer.Ordinal);

        foreach (var importItem in items)
        {
            if (importItem.Request.NegativeMarks != 0)
            {
                throw new ArgumentException($"Row {importItem.SourceRowNumber}: Coding questions do not support negative marks.");
            }

            var question = importItem.Request.ToCodingEntity();
            var testCases = importItem.Request.TestCases.ToTestCaseEntities();

            try
            {
                ValidateCodingQuestion(question, testCases, languageLookup, comparisonModes);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                throw new ArgumentException($"Row {importItem.SourceRowNumber}: {ex.Message}");
            }

            question.CodingQuestionId = question.CodingQuestionId == Guid.Empty ? Guid.NewGuid() : question.CodingQuestionId;
            question.IsActive = true;
            question.CreatedAt = DateTime.UtcNow;
            question.ModifiedAt = DateTime.UtcNow;
            question.Version = question.Version <= 0 ? 1 : question.Version;
            question.TestCases = testCases;

            var fingerprint = BuildCodingDuplicateFingerprint(question);
            if (!importFingerprints.Add(fingerprint))
            {
                continue;
            }

            preparedQuestions.Add((importItem.InputOrder, question));
        }

        if (preparedQuestions.Count == 0)
        {
            return [];
        }

        var codingCollegeIds = preparedQuestions
            .Select(item => item.Question.CollegeId)
            .Distinct()
            .ToList();

        var normalizedCodingTitles = preparedQuestions
            .Select(item => NormalizeForLookup(item.Question.QuestionTitle))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct()
            .ToList();

        var existingCodingQuestions = await _context.CodingQuestions
            .AsNoTracking()
            .Where(question =>
                question.IsActive &&
                codingCollegeIds.Contains(question.CollegeId) &&
                normalizedCodingTitles.Contains(question.QuestionTitle.ToLower()))
            .ToListAsync();

        var existingFingerprints = existingCodingQuestions
            .Select(BuildCodingDuplicateFingerprint)
            .ToHashSet(StringComparer.Ordinal);

        var uniqueQuestionsToCreate = preparedQuestions
            .Where(item => !existingFingerprints.Contains(BuildCodingDuplicateFingerprint(item.Question)))
            .ToList();

        if (uniqueQuestionsToCreate.Count == 0)
        {
            return [];
        }

        _context.CodingQuestions.AddRange(uniqueQuestionsToCreate.Select(item => item.Question));

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Unable to save the coding question to the question bank.", ex);
        }

        return uniqueQuestionsToCreate
            .Select(item => (item.InputOrder, item.Question.ToRecord()))
            .ToList();
    }

    private static string NormalizeQuestionType(string? questionType)
    {
        return QuestionAnswerJsonHelper.NormalizeSingleValue(questionType)?.ToLowerInvariant() ?? string.Empty;
    }

    private static void ValidateCodingQuestion(
        CodingQuestion question,
        List<TestCase> testCases,
        IReadOnlyDictionary<string, CodingLanguage> languageLookup,
        ISet<short> comparisonModes)
    {
        if (question.CollegeId == Guid.Empty)
        {
            throw new ArgumentException("CollegeId is required.");
        }

        if (string.IsNullOrWhiteSpace(question.CreatedBy))
        {
            throw new ArgumentException("CreatedBy is required.");
        }

        if (string.IsNullOrWhiteSpace(question.QuestionTitle))
        {
            throw new ArgumentException("Question title is required for coding questions.");
        }

        if (string.IsNullOrWhiteSpace(question.ProblemStatement))
        {
            throw new ArgumentException("Problem statement is required for coding questions.");
        }

        if (question.QuestionType != "coding")
        {
            throw new ArgumentException("Question type must be 'coding' for coding questions.");
        }

        if (question.Marks < 0)
        {
            throw new ArgumentException("Marks cannot be negative.");
        }

        if (question.DefaultTimeLimitMs <= 0)
        {
            throw new ArgumentException("Default time limit must be greater than zero.");
        }

        if (question.DefaultMemoryLimitKb <= 0)
        {
            throw new ArgumentException("Default memory limit must be greater than zero.");
        }

        if (question.DefaultMaxCodeSizeKb <= 0)
        {
            throw new ArgumentException("Default max code size must be greater than zero.");
        }

        if (!string.IsNullOrWhiteSpace(question.DefaultLanguageCode) &&
            !languageLookup.ContainsKey(question.DefaultLanguageCode))
        {
            throw new ArgumentException($"Default language '{question.DefaultLanguageCode}' was not found or is inactive.");
        }

        if (testCases.Count == 0)
        {
            throw new ArgumentException("At least one test case is required for coding questions.");
        }

        foreach (var testCase in testCases)
        {
            if (!AllowedCodingInputFormats.Contains(testCase.InputFormat))
            {
                throw new ArgumentException("Test case input format must be one of 'stdin', 'json', or 'function_args'.");
            }

            if (string.IsNullOrWhiteSpace(testCase.ExpectedOutput))
            {
                throw new ArgumentException("Expected output is required for each coding test case.");
            }

            if (testCase.NumericTolerance.HasValue && testCase.NumericTolerance.Value < 0)
            {
                throw new ArgumentException("Numeric tolerance cannot be negative.");
            }

            if (testCase.TimeLimitMs.HasValue && testCase.TimeLimitMs.Value <= 0)
            {
                throw new ArgumentException("Test case time limit must be greater than zero when provided.");
            }

            if (testCase.MemoryLimitKb.HasValue && testCase.MemoryLimitKb.Value <= 0)
            {
                throw new ArgumentException("Test case memory limit must be greater than zero when provided.");
            }

            var comparisonModeId = testCase.ComparisonMode == 0
                ? DefaultCodingComparisonMode
                : testCase.ComparisonMode;

            if (!comparisonModes.Contains((short)comparisonModeId))
            {
                throw new ArgumentException($"Comparison mode '{comparisonModeId}' was not found.");
            }

            testCase.ComparisonMode = comparisonModeId;
        }
    }

    private async Task EnsureCodingQuestionIsNotInLiveAssessmentAsync(Guid codingQuestionId)
    {
        var liveAssessmentLinkExists = await _context.AssessmentCodingQuestions
            .AsNoTracking()
            .Join(
                _context.Assessments.AsNoTracking(),
                assessmentQuestion => assessmentQuestion.AssessmentId,
                assessment => assessment.AssessmentId,
                (assessmentQuestion, assessment) => new
                {
                    assessmentQuestion.CodingQuestionId,
                    assessment.AssessmentStatus
                })
            .AnyAsync(item =>
                item.CodingQuestionId == codingQuestionId &&
                item.AssessmentStatus == AssessmentStatus.Live);

        if (liveAssessmentLinkExists)
        {
            throw new InvalidOperationException("This question cannot be edited because it is included in a live assessment.");
        }
    }

    private static string BuildCodingDuplicateFingerprint(CodingQuestion question)
    {
        return string.Join("|", [
            question.CollegeId.ToString("D"),
            NormalizeForLookup(question.QuestionTitle),
            NormalizeForLookup(question.ProblemStatement),
            NormalizeTopicTagsForLookup(question.TopicTag),
            NormalizeForLookup(question.QuestionType),
            NormalizeForLookup(question.DefaultLanguageCode),
            question.Marks.ToString("0.##"),
            question.DifficultyLevel.ToString()
        ]);
    }

    private static bool HasMinimumValidOptions(string? options, int minimumCount)
    {
        var parsedOptions = DeserializeOptions(options);
        return parsedOptions is not null &&
               parsedOptions.Count >= minimumCount &&
               parsedOptions.All(option => !string.IsNullOrWhiteSpace(option));
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
        var parsedOptions = DeserializeOptions(options) ?? [];
        return string.Join("~", parsedOptions.Select(NormalizeForLookup));
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
        catch
        {
            return null;
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
