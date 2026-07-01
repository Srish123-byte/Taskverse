using Taskverse.API.Assessments.Service.Models;
using Taskverse.Data.DataAccess;
using Taskverse.Data.Utilities;
using System.Text.Json;

namespace Taskverse.API.Assessments.Service.Mappings;

public static class QuestionMappings
{
    private const int DefaultCodingTimeLimitMs = 3000;
    private const int DefaultCodingMemoryLimitKb = 262144;
    private const int DefaultCodingMaxCodeSizeKb = 512;

    public static Question ToEntity(this CreateQuestionRequest request)
    {
        var normalizedOptions = request.Options?
            .Select(QuestionAnswerJsonHelper.NormalizeSingleValue)
            .Where(option => !string.IsNullOrWhiteSpace(option))
            .ToList();
        var normalizedTopicTags = NormalizeTopicTags(request.TopicTag);
        var normalizedCorrectAnswers = request.CorrectAnswers?.Count > 0
            ? QuestionAnswerJsonHelper.NormalizeAnswerValues(request.CorrectAnswers)
            : QuestionAnswerJsonHelper.ParseStoredAnswers(request.Answer);

        return new Question
        {
            CollegeId = request.CollegeId,
            SubjectId = request.SubjectId,
            Stream = QuestionAnswerJsonHelper.NormalizeSingleValue(request.Stream),
            Subject = QuestionAnswerJsonHelper.NormalizeSingleValue(request.Subject),
            TopicId = request.TopicId,
            Topic = QuestionAnswerJsonHelper.NormalizeSingleValue(request.Topic),
            TopicTag = normalizedTopicTags,
            QuestionType = QuestionAnswerJsonHelper.NormalizeSingleValue(request.QuestionType) ?? string.Empty,
            QuestionText = QuestionAnswerJsonHelper.NormalizeSingleValue(request.QuestionText) ?? string.Empty,
            Options = normalizedOptions is null ? null : JsonSerializer.Serialize(normalizedOptions),
            Answer = QuestionAnswerJsonHelper.SerializeAnswers(normalizedCorrectAnswers),
            Explanation = QuestionAnswerJsonHelper.NormalizeSingleValue(request.Explanation),
            Marks = request.Marks,
            NegativeMarks = request.NegativeMarks,
            DifficultyLevel = request.DifficultyLevel,
            CreatedBy = request.CreatedBy
        };
    }

    public static QuestionRecord ToRecord(this Question question)
    {
        return new QuestionRecord(
            question.QuestionId,
            question.CollegeId,
            question.SubjectId,
            question.TopicId,
            question.Stream,
            question.Subject,
            question.Topic,
            question.TopicTag?.ToList(),
            question.QuestionType,
            question.QuestionText,
            DeserializeOptions(question.Options),
            question.Answer,
            question.Explanation,
            question.Marks,
            question.NegativeMarks,
            question.DifficultyLevel,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            question.Version,
            question.CreatedBy,
            UtcDateTime.Normalize(question.CreatedAt),
            UtcDateTime.Normalize(question.ModifiedAt));
    }

    public static QuestionRecord ToRecord(this CodingQuestion question)
    {
        return new QuestionRecord(
            question.CodingQuestionId,
            question.CollegeId,
            null,
            null,
            null,
            null,
            null,
            question.TopicTag?.ToList(),
            question.QuestionType,
            question.ProblemStatement,
            null,
            null,
            question.Explanation,
            question.Marks,
            0,
            question.DifficultyLevel,
            question.QuestionTitle,
            question.ProblemStatement,
            question.DetailedDescription,
            question.InputFormat,
            question.OutputFormat,
            question.ConstraintsText,
            DeserializeExamples(question.Examples),
            question.DefaultLanguageCode,
            question.DefaultTimeLimitMs,
            question.DefaultMemoryLimitKb,
            question.DefaultMaxCodeSizeKb,
            question.TestCases
                .Where(testCase => testCase.IsActive)
                .OrderBy(testCase => testCase.IsSample ? 0 : 1)
                .ThenBy(testCase => testCase.CreatedAt)
                .Select(testCase => testCase.ToRecord())
                .ToList(),
            question.Version,
            question.CreatedBy ?? string.Empty,
            UtcDateTime.Normalize(question.CreatedAt),
            UtcDateTime.Normalize(question.ModifiedAt));
    }

    public static CodingQuestion ToCodingEntity(this CreateQuestionRequest request)
    {
        var normalizedQuestionType = QuestionAnswerJsonHelper.NormalizeSingleValue(request.QuestionType)?.ToLowerInvariant() ?? string.Empty;
        var normalizedTopicTags = NormalizeTopicTags(request.TopicTag);

        return new CodingQuestion
        {
            CollegeId = request.CollegeId,
            QuestionTitle = QuestionAnswerJsonHelper.NormalizeSingleValue(request.QuestionTitle) ?? string.Empty,
            ProblemStatement = QuestionAnswerJsonHelper.NormalizeSingleValue(request.ProblemStatement ?? request.QuestionText) ?? string.Empty,
            DetailedDescription = QuestionAnswerJsonHelper.NormalizeSingleValue(request.DetailedDescription),
            DifficultyLevel = request.DifficultyLevel,
            QuestionType = normalizedQuestionType,
            TopicTag = normalizedTopicTags,
            InputFormat = QuestionAnswerJsonHelper.NormalizeSingleValue(request.InputFormat),
            OutputFormat = QuestionAnswerJsonHelper.NormalizeSingleValue(request.OutputFormat),
            ConstraintsText = QuestionAnswerJsonHelper.NormalizeSingleValue(request.ConstraintsText),
            Explanation = QuestionAnswerJsonHelper.NormalizeSingleValue(request.Explanation),
            Examples = SerializeExamples(request.Examples),
            DefaultLanguageCode = QuestionAnswerJsonHelper.NormalizeSingleValue(request.DefaultLanguageCode)?.ToLowerInvariant(),
            DefaultTimeLimitMs = request.DefaultTimeLimitMs ?? DefaultCodingTimeLimitMs,
            DefaultMemoryLimitKb = request.DefaultMemoryLimitKb ?? DefaultCodingMemoryLimitKb,
            DefaultMaxCodeSizeKb = request.DefaultMaxCodeSizeKb ?? DefaultCodingMaxCodeSizeKb,
            Marks = request.Marks,
            IsActive = true,
            CreatedBy = QuestionAnswerJsonHelper.NormalizeSingleValue(request.CreatedBy),
            ModifiedBy = null
        };
    }

    public static List<TestCase> ToTestCaseEntities(this IEnumerable<CodingTestCaseRequest>? requests)
    {
        return (requests ?? [])
            .Select(request => new TestCase
            {
                TestCaseId = Guid.NewGuid(),
                InputFormat = QuestionAnswerJsonHelper.NormalizeSingleValue(request.InputFormat)?.ToLowerInvariant() ?? "stdin",
                InputData = request.InputData,
                ExpectedOutput = request.ExpectedOutput,
                ComparisonMode = request.ComparisonMode,
                NumericTolerance = request.NumericTolerance,
                IsHidden = request.IsHidden,
                IsSample = request.IsSample,
                IsActive = true,
                TimeLimitMs = request.TimeLimitMs,
                MemoryLimitKb = request.MemoryLimitKb,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            })
            .ToList();
    }

    public static CodingTestCaseRecord ToRecord(this TestCase testCase)
    {
        return new CodingTestCaseRecord(
            testCase.TestCaseId,
            testCase.InputFormat,
            testCase.InputData,
            testCase.ExpectedOutput,
            testCase.ComparisonMode,
            testCase.NumericTolerance,
            testCase.IsHidden,
            testCase.IsSample,
            testCase.TimeLimitMs,
            testCase.MemoryLimitKb);
    }

    public static QuestionTopicCatalogRecord ToCatalogRecord(this Topic topic)
    {
        return new QuestionTopicCatalogRecord(
            topic.TopicId,
            topic.TopicName);
    }

    public static QuestionSubjectCatalogRecord ToCatalogRecord(this Subject subject, List<QuestionTopicCatalogRecord> topics)
    {
        return new QuestionSubjectCatalogRecord(
            subject.SubjectId,
            subject.SubjectName,
            topics);
    }

    public static QuestionClassificationEntryRecord ToClassificationEntryRecord(this Subject subject, Topic? topic = null)
    {
        return new QuestionClassificationEntryRecord(
            subject.SubjectId,
            subject.SubjectName,
            topic?.TopicId,
            topic?.TopicName);
    }

    public static void ApplyUpdates(this Question target, Question source)
    {
        target.CollegeId = source.CollegeId;
        target.SubjectId = source.SubjectId;
        target.Stream = source.Stream;
        target.Subject = source.Subject;
        target.TopicId = source.TopicId;
        target.Topic = source.Topic;
        target.TopicTag = source.TopicTag;
        target.QuestionType = source.QuestionType;
        target.QuestionText = source.QuestionText;
        target.Options = source.Options;
        target.Answer = source.Answer;
        target.Explanation = source.Explanation;
        target.Marks = source.Marks;
        target.NegativeMarks = source.NegativeMarks;
        target.DifficultyLevel = source.DifficultyLevel;
    }

    public static void ApplyUpdates(this CodingQuestion target, CodingQuestion source)
    {
        target.QuestionTitle = source.QuestionTitle;
        target.ProblemStatement = source.ProblemStatement;
        target.DetailedDescription = source.DetailedDescription;
        target.DifficultyLevel = source.DifficultyLevel;
        target.QuestionType = source.QuestionType;
        target.TopicTag = source.TopicTag;
        target.InputFormat = source.InputFormat;
        target.OutputFormat = source.OutputFormat;
        target.ConstraintsText = source.ConstraintsText;
        target.Explanation = source.Explanation;
        target.Examples = source.Examples;
        target.DefaultLanguageCode = source.DefaultLanguageCode;
        target.DefaultTimeLimitMs = source.DefaultTimeLimitMs;
        target.DefaultMemoryLimitKb = source.DefaultMemoryLimitKb;
        target.DefaultMaxCodeSizeKb = source.DefaultMaxCodeSizeKb;
        target.Marks = source.Marks;
    }

    public static List<Guid> NormalizeQuestionIds(this IEnumerable<Guid> questionIds)
    {
        return questionIds
            .Where(questionId => questionId != Guid.Empty)
            .Distinct()
            .ToList();
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

    private static string[] NormalizeTopicTags(IEnumerable<string>? values)
    {
        return (values ?? [])
            .Select(QuestionAnswerJsonHelper.NormalizeSingleValue)
            .OfType<string>()
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static List<CodingQuestionExampleRecord>? DeserializeExamples(string? examples)
    {
        if (string.IsNullOrWhiteSpace(examples))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<CodingQuestionExampleRecord>>(examples);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? SerializeExamples(IEnumerable<CodingQuestionExampleRequest>? examples)
    {
        var normalizedExamples = (examples ?? [])
            .Select(example => new CodingQuestionExampleRecord(
                QuestionAnswerJsonHelper.NormalizeSingleValue(example.Input),
                QuestionAnswerJsonHelper.NormalizeSingleValue(example.Output),
                QuestionAnswerJsonHelper.NormalizeSingleValue(example.Explanation)))
            .Where(example =>
                !string.IsNullOrWhiteSpace(example.Input) ||
                !string.IsNullOrWhiteSpace(example.Output) ||
                !string.IsNullOrWhiteSpace(example.Explanation))
            .ToList();

        return normalizedExamples.Count == 0
            ? null
            : JsonSerializer.Serialize(normalizedExamples);
    }
}
