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

        var subjectByName = await FindExistingSubjectByNameAsync(context, normalizedSubjectName);

        if (subjectByName is null)
        {
            // Double-check the database immediately before creating to avoid duplicate inserts
            // when the same subject name is encountered repeatedly during bulk import.
            subjectByName = await FindPersistedSubjectByNameAsync(context, normalizedSubjectName);
        }

        if (subjectByName is null)
        {
            subjectByName = new Subject
            {
                SubjectId = Guid.NewGuid(),
                SubjectName = subjectName!.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            context.Subjects.Add(subjectByName);
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

        var topics = await FindExistingTopicsByNameAsync(context, normalizedTopicName, subjectId);
        if (topics.Count == 0)
        {
            // Double-check the persisted database immediately before creating the topic.
            topics = await FindPersistedTopicsByNameAsync(context, normalizedTopicName, subjectId);
        }

        if (topics.Count == 0)
        {
            if (!subjectId.HasValue || subjectId == Guid.Empty)
            {
                throw new KeyNotFoundException($"Topic '{topicName}' was not found and no subject was available to create it.");
            }

            var createdTopic = new Topic
            {
                TopicId = Guid.NewGuid(),
                SubjectId = subjectId.Value,
                TopicName = topicName!.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            createdTopic.Subject = await context.Subjects.FindAsync(subjectId.Value)
                ?? throw new KeyNotFoundException($"Subject with id '{subjectId}' was not found.");
            context.Topics.Add(createdTopic);

            return createdTopic;
        }

        if (topics.Count > 1)
        {
            throw new InvalidOperationException("Topic name is ambiguous. Specify a subject or topic id.");
        }

        return topics[0];
    }

    private static async Task<Subject?> FindExistingSubjectByNameAsync(
        TaskverseContext context,
        string normalizedSubjectName)
    {
        var trackedSubject = context.Subjects.Local.FirstOrDefault(subject =>
            subject.IsActive &&
            string.Equals(subject.SubjectName, normalizedSubjectName, StringComparison.OrdinalIgnoreCase));

        if (trackedSubject is not null)
        {
            return trackedSubject;
        }

        return await FindPersistedSubjectByNameAsync(context, normalizedSubjectName);
    }

    private static Task<Subject?> FindPersistedSubjectByNameAsync(
        TaskverseContext context,
        string normalizedSubjectName)
    {
        return context.Subjects
            .AsNoTracking()
            .FirstOrDefaultAsync(subject =>
                subject.IsActive &&
                subject.SubjectName.ToLower() == normalizedSubjectName);
    }

    private static async Task<List<Topic>> FindExistingTopicsByNameAsync(
        TaskverseContext context,
        string normalizedTopicName,
        Guid? subjectId)
    {
        var trackedTopicQuery = context.Topics.Local
            .Where(topic =>
                topic.IsActive &&
                string.Equals(topic.TopicName, normalizedTopicName, StringComparison.OrdinalIgnoreCase));

        if (subjectId.HasValue && subjectId.Value != Guid.Empty)
        {
            trackedTopicQuery = trackedTopicQuery.Where(topic => topic.SubjectId == subjectId.Value);
        }

        var trackedTopics = trackedTopicQuery.ToList();
        if (trackedTopics.Count > 0)
        {
            foreach (var trackedTopic in trackedTopics.Where(trackedTopic => trackedTopic.Subject is null))
            {
                trackedTopic.Subject = await context.Subjects.FindAsync(trackedTopic.SubjectId);
            }

            return trackedTopics;
        }

        return await FindPersistedTopicsByNameAsync(context, normalizedTopicName, subjectId);
    }

    private static async Task<List<Topic>> FindPersistedTopicsByNameAsync(
        TaskverseContext context,
        string normalizedTopicName,
        Guid? subjectId)
    {
        var query = context.Topics
            .AsNoTracking()
            .Include(topic => topic.Subject)
            .Where(topic => topic.IsActive && topic.TopicName.ToLower() == normalizedTopicName);

        if (subjectId.HasValue && subjectId.Value != Guid.Empty)
        {
            query = query.Where(topic => topic.SubjectId == subjectId.Value);
        }

        return await query.ToListAsync();
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
}
