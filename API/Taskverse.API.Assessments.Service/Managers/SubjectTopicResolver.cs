using Microsoft.EntityFrameworkCore;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.Assessments.Service.Managers;

internal static class SubjectTopicResolver
{
    internal sealed record Resolution(Subject Subject, Topic Topic);

    public static async Task<Resolution> ResolveAsync(
        TaskverseContext context,
        Guid? subjectId,
        string? subjectName,
        Guid? topicId,
        string? topicName)
    {
        var subject = await ResolveSubjectAsync(context, subjectId, subjectName);
        var topic = await ResolveTopicAsync(context, topicId, topicName, subject?.SubjectId);

        subject ??= topic.Subject;

        if (subject is null)
        {
            throw new KeyNotFoundException("Subject was not found.");
        }

        if (topic.SubjectId != subject.SubjectId)
        {
            throw new InvalidOperationException("Topic does not belong to the specified subject.");
        }

        return new Resolution(subject, topic);
    }

    public static async Task PopulateQuestionSubjectTopicIdsAsync(
        TaskverseContext context,
        IEnumerable<Question> questions)
    {
        var questionList = questions.ToList();
        if (questionList.Count == 0)
        {
            return;
        }

        var subjectNames = questionList
            .Select(question => Normalize(question.Subject))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .ToList();

        var topicNames = questionList
            .Select(question => Normalize(question.Topic))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .ToList();

        var subjects = await context.Subjects
            .AsNoTracking()
            .Where(subject => subjectNames.Contains(subject.SubjectName.ToLower()))
            .ToListAsync();

        var topics = await context.Topics
            .AsNoTracking()
            .Where(topic => topicNames.Contains(topic.TopicName.ToLower()))
            .ToListAsync();

        foreach (var question in questionList)
        {
            var normalizedSubjectName = Normalize(question.Subject);
            var normalizedTopicName = Normalize(question.Topic);

            var subject = subjects.FirstOrDefault(item =>
                string.Equals(item.SubjectName, normalizedSubjectName, StringComparison.OrdinalIgnoreCase));

            Topic? topic = null;
            if (subject is not null)
            {
                topic = topics.FirstOrDefault(item =>
                    item.SubjectId == subject.SubjectId &&
                    string.Equals(item.TopicName, normalizedTopicName, StringComparison.OrdinalIgnoreCase));
            }

            question.SubjectId = subject?.SubjectId;
            question.TopicId = topic?.TopicId;
        }
    }

    private static async Task<Subject?> ResolveSubjectAsync(
        TaskverseContext context,
        Guid? subjectId,
        string? subjectName)
    {
        if (subjectId.HasValue && subjectId.Value != Guid.Empty)
        {
            var subjectById = await context.Subjects
                .AsNoTracking()
                .FirstOrDefaultAsync(subject => subject.SubjectId == subjectId.Value && subject.IsActive);

            if (subjectById is null)
            {
                throw new KeyNotFoundException($"Subject with id '{subjectId}' was not found.");
            }

            return subjectById;
        }

        var normalizedSubjectName = Normalize(subjectName);
        if (string.IsNullOrWhiteSpace(normalizedSubjectName))
        {
            return null;
        }

        var subjectByName = await context.Subjects
            .AsNoTracking()
            .FirstOrDefaultAsync(subject => subject.IsActive && subject.SubjectName.ToLower() == normalizedSubjectName);

        if (subjectByName is null)
        {
            throw new KeyNotFoundException($"Subject '{subjectName}' was not found.");
        }

        return subjectByName;
    }

    private static async Task<Topic> ResolveTopicAsync(
        TaskverseContext context,
        Guid? topicId,
        string? topicName,
        Guid? subjectId)
    {
        if (topicId.HasValue && topicId.Value != Guid.Empty)
        {
            var topicById = await context.Topics
                .AsNoTracking()
                .Include(topic => topic.Subject)
                .FirstOrDefaultAsync(topic => topic.TopicId == topicId.Value && topic.IsActive);

            if (topicById is null)
            {
                throw new KeyNotFoundException($"Topic with id '{topicId}' was not found.");
            }

            return topicById;
        }

        var normalizedTopicName = Normalize(topicName);
        if (string.IsNullOrWhiteSpace(normalizedTopicName))
        {
            throw new KeyNotFoundException("Topic is required.");
        }

        var query = context.Topics
            .AsNoTracking()
            .Include(topic => topic.Subject)
            .Where(topic => topic.IsActive && topic.TopicName.ToLower() == normalizedTopicName);

        if (subjectId.HasValue && subjectId.Value != Guid.Empty)
        {
            query = query.Where(topic => topic.SubjectId == subjectId.Value);
        }

        var topics = await query.ToListAsync();
        if (topics.Count == 0)
        {
            throw new KeyNotFoundException($"Topic '{topicName}' was not found.");
        }

        if (topics.Count > 1)
        {
            throw new InvalidOperationException("Topic name is ambiguous. Specify a subject or topic id.");
        }

        return topics[0];
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
}
