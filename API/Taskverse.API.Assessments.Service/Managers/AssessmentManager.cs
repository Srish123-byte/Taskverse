using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Taskverse.API.Assessments.Service.Mappings;
using Taskverse.API.Assessments.Service.Models;
using Taskverse.Business.Enums;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.Assessments.Service.Managers;

public class AssessmentManager : IAssessmentManager
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 10;
    private const int MaximumPageSize = 100;

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

        var classification = await ResolveAssessmentClassificationAsync(assessment);
        ApplyClassification(assessment, classification);

        var normalizedQuestionIds = questionIds.NormalizeQuestionIds();
        var normalizedAssignedBatchIds = NormalizeAssignedBatchIds(assessment.AssignedBatchIds);

        await ValidateAssignedBatchesAsync(assessment.CollegeId, normalizedAssignedBatchIds);

        assessment.AssignedBatchIds = normalizedAssignedBatchIds;

        var questions = await LoadAndValidateQuestionsForCreateAsync(
            assessment,
            normalizedQuestionIds,
            classification.Subject.SubjectName,
            classification.Topic.TopicName);

        ValidateQuestionBudget(assessment, questions);

        PrepareAssessmentForCreation(assessment, questions);
        assessment.AssessmentQuestions = BuildAssessmentQuestions(assessment.AssessmentId, questions, normalizedQuestionIds);

        AssignQuestionsToAssessment(questions, assessment.AssessmentId);

        _context.Assessments.Add(assessment);
        await SaveChangesWithWrapAsync("Unable to save the assessment.");

        return assessment;
    }

    public async Task DeleteAssessment(Guid assessmentId, DeleteAssessmentRequest request)
    {
        ValidateDeleteAssessmentRequest(request);

        var assessment = await GetAssessmentByIdAsync(assessmentId);
        EnsureDeleteAuthorized(assessment, request);

        var deletedAt = DateTime.UtcNow;

        assessment.AssessmentStatus = AssessmentStatus.Soft_Delete;
        assessment.SoftDeletedAt = deletedAt;
        assessment.SoftDeletedBy = request.DeletedBy.Trim();
        assessment.ModifiedAt = deletedAt;

        await SaveChangesWithWrapAsync("Unable to delete the assessment.");
    }

    public async Task<Assessment> PublishAssessment(Guid assessmentId)
    {
        var assessment = await _context.Assessments
            .Include(item => item.AssessmentQuestions)
            .Include(item => item.Subject)
            .Include(item => item.Topic)
            .FirstOrDefaultAsync(item => item.AssessmentId == assessmentId);

        if (assessment is null)
        {
            throw new KeyNotFoundException($"Assessment '{assessmentId}' was not found.");
        }

        EnsureAssessmentCanBePublished(assessment);

        var questions = await LoadQuestionsForPublishAsync(assessment);
        ValidateQuestionsForPublish(assessment, questions);
        ValidateQuestionBudget(assessment, questions);

        PrepareAssessmentForPublish(assessment, questions);

        if (assessment.AssessmentQuestions.Count == 0)
        {
            assessment.AssessmentQuestions = BuildAssessmentQuestions(
                assessment.AssessmentId,
                questions,
                questions.Select(question => question.QuestionId).ToList());
        }

        AssignQuestionsToAssessment(questions, assessment.AssessmentId, updateOnlyWhenChanged: true);

        await SaveChangesWithWrapAsync("Unable to publish the assessment.");

        return assessment;
    }

    public async Task<PagedAssessmentQuestionListRecord> GetAssessmentQuestionList(
        Guid assessmentId,
        int pageNumber,
        int pageSize)
    {
        var safePageNumber = pageNumber > 0 ? pageNumber : DefaultPageNumber;
        var safePageSize = pageSize is > 0 and <= MaximumPageSize ? pageSize : DefaultPageSize;

        var assessment = await _context.Assessments
            .AsNoTracking()
            .Include(item => item.AssessmentQuestions)
            .FirstOrDefaultAsync(item => item.AssessmentId == assessmentId);

        if (assessment is null)
        {
            throw new KeyNotFoundException($"Assessment '{assessmentId}' was not found.");
        }

        var orderedQuestionIds = assessment.AssessmentQuestions
            .OrderBy(item => item.DisplayOrder)
            .Select(item => item.QuestionId)
            .ToList();

        var pagedQuestionIds = orderedQuestionIds
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        var questions = await _context.Questions
            .AsNoTracking()
            .Where(question => pagedQuestionIds.Contains(question.QuestionId))
            .ToDictionaryAsync(question => question.QuestionId);

        var displayOrderLookup = assessment.AssessmentQuestions
            .ToDictionary(item => item.QuestionId, item => item.DisplayOrder);

        var items = pagedQuestionIds
            .Where(questions.ContainsKey)
            .Select(questionId => questions[questionId].ToQuestionListItemRecord(displayOrderLookup.GetValueOrDefault(questionId)))
            .ToList();

        return new PagedAssessmentQuestionListRecord(
            items,
            orderedQuestionIds.Count,
            safePageNumber,
            safePageSize);
    }

    public async Task<List<StudentAssessmentListItemRecord>> GetStudentAssessments(
        Guid studentUserId,
        IReadOnlyCollection<string> assessmentStatuses)
    {
        if (studentUserId == Guid.Empty)
        {
            throw new ArgumentException("Student user id is required.");
        }

        var normalizedStatuses = NormalizeStudentAssessmentStatuses(assessmentStatuses);

        var student = await _context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == studentUserId);

        if (student is null)
        {
            throw new KeyNotFoundException($"Student profile was not found for user '{studentUserId}'.");
        }

        if (!student.BatchId.HasValue || student.BatchId.Value == Guid.Empty)
        {
            return [];
        }

        return normalizedStatuses.Contains(nameof(AssessmentStatus.Completed))
            ? await GetCompletedStudentAssessmentsAsync(student, student.BatchId.Value)
            : await GetActiveStudentAssessmentsAsync(student, student.BatchId.Value, normalizedStatuses);
    }

    public async Task<StudentAssessmentDetailRecord> GetStudentAssessmentDetail(Guid assessmentId, Guid studentUserId)
    {
        if (assessmentId == Guid.Empty)
        {
            throw new ArgumentException("Assessment id is required.");
        }

        if (studentUserId == Guid.Empty)
        {
            throw new ArgumentException("Student user id is required.");
        }

        var student = await _context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == studentUserId);

        if (student is null)
        {
            throw new KeyNotFoundException($"Student profile was not found for user '{studentUserId}'.");
        }

        if (!student.BatchId.HasValue || student.BatchId.Value == Guid.Empty)
        {
            throw new KeyNotFoundException($"Assessment '{assessmentId}' was not found for the current student.");
        }

        var assessment = await BuildStudentAssessmentQuery(student.CollegeId, student.BatchId.Value)
            .Include(item => item.AssessmentQuestions)
            .FirstOrDefaultAsync(item =>
                item.AssessmentId == assessmentId &&
                (item.AssessmentStatus == AssessmentStatus.Scheduled ||
                 item.AssessmentStatus == AssessmentStatus.Live));

        if (assessment is null)
        {
            throw new KeyNotFoundException($"Assessment '{assessmentId}' was not found for the current student.");
        }

        return assessment.ToStudentAssessmentDetailRecord(assessment.AssessmentQuestions.Count);
    }

    public async Task<StudentAssessmentStartRecord> StartStudentAssessment(Guid assessmentId, Guid studentUserId)
    {
        if (assessmentId == Guid.Empty)
        {
            throw new ArgumentException("Assessment id is required.");
        }

        if (studentUserId == Guid.Empty)
        {
            throw new ArgumentException("Student user id is required.");
        }

        var student = await _context.Students
            .FirstOrDefaultAsync(item => item.UserId == studentUserId);

        if (student is null)
        {
            throw new KeyNotFoundException($"Student profile was not found for user '{studentUserId}'.");
        }

        if (!student.BatchId.HasValue || student.BatchId.Value == Guid.Empty)
        {
            throw new KeyNotFoundException($"Assessment '{assessmentId}' was not found for the current student.");
        }

        var assessment = await BuildStudentAssessmentQuery(student.CollegeId, student.BatchId.Value)
            .Include(item => item.AssessmentQuestions)
            .FirstOrDefaultAsync(item =>
                item.AssessmentId == assessmentId &&
                (item.AssessmentStatus == AssessmentStatus.Scheduled ||
                 item.AssessmentStatus == AssessmentStatus.Live));

        if (assessment is null)
        {
            throw new KeyNotFoundException($"Assessment '{assessmentId}' was not found for the current student.");
        }

        var existingAttempt = await _context.Attempts
            .Where(item => item.AssessmentId == assessmentId && item.StudentId == student.StudentId)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync();

        if (existingAttempt is not null)
        {
            if (existingAttempt.AttemptStatus is AttemptStatus.Submitted or AttemptStatus.Auto_Submitted)
            {
                throw new InvalidOperationException("This assessment has already been submitted by the current student.");
            }

            if (!existingAttempt.StartedAt.HasValue)
            {
                existingAttempt.StartedAt = DateTime.UtcNow;
            }

            existingAttempt.AttemptStatus = AttemptStatus.In_Progress;
            await SaveChangesWithWrapAsync("Unable to start the assessment attempt.");
            return existingAttempt.ToStudentAssessmentStartRecord();
        }

        var startedAt = DateTime.UtcNow;
        var totalQuestions = assessment.AssessmentQuestions.Count;

        var attempt = new Attempt
        {
            AttemptId = Guid.NewGuid(),
            AssessmentId = assessment.AssessmentId,
            StudentId = student.StudentId,
            StartedAt = startedAt,
            AttemptStatus = AttemptStatus.In_Progress,
            TotalQuestions = totalQuestions,
            AttemptedQuestions = 0,
            CorrectAnswers = 0,
            WrongAnswers = 0,
            UnansweredQuestions = totalQuestions,
            TotalScore = 0,
            Percentage = 0,
            TimeTakenSeconds = 0,
            IsPassed = false,
            CreatedAt = startedAt
        };

        _context.Attempts.Add(attempt);
        await SaveChangesWithWrapAsync("Unable to start the assessment attempt.");

        return attempt.ToStudentAssessmentStartRecord();
    }

    private async Task<SubjectTopicResolver.Resolution> ResolveAssessmentClassificationAsync(Assessment assessment)
    {
        return await SubjectTopicResolver.ResolveAsync(
            _context,
            assessment.SubjectId,
            assessment.SubjectName,
            assessment.TopicId,
            assessment.TopicName);
    }

    private static void ApplyClassification(Assessment assessment, SubjectTopicResolver.Resolution classification)
    {
        assessment.SubjectId = classification.Subject.SubjectId;
        assessment.Subject = classification.Subject;
        assessment.SubjectName = classification.Subject.SubjectName;
        assessment.TopicId = classification.Topic.TopicId;
        assessment.Topic = classification.Topic;
        assessment.TopicName = classification.Topic.TopicName;
    }

    private static Guid[] NormalizeAssignedBatchIds(IEnumerable<Guid>? assignedBatchIds)
    {
        return (assignedBatchIds ?? [])
            .Where(batchId => batchId != Guid.Empty)
            .Distinct()
            .ToArray();
    }

    private async Task ValidateAssignedBatchesAsync(Guid collegeId, IReadOnlyCollection<Guid> batchIds)
    {
        if (batchIds.Count == 0)
        {
            return;
        }

        var validBatchIds = await _context.Batches
            .AsNoTracking()
            .Where(batch => batch.CollegeId == collegeId && batchIds.Contains(batch.BatchId))
            .Select(batch => batch.BatchId)
            .ToListAsync();

        var invalidBatchIds = batchIds.Except(validBatchIds).ToArray();
        if (invalidBatchIds.Length > 0)
        {
            throw new InvalidOperationException(
                $"Batch id(s) do not belong to this college or were not found: {string.Join(", ", invalidBatchIds)}.");
        }
    }

    private async Task<List<Question>> LoadAndValidateQuestionsForCreateAsync(
        Assessment assessment,
        List<Guid> questionIds,
        string subjectName,
        string topicName)
    {
        if (questionIds.Count == 0)
        {
            throw new ArgumentException("At least one valid question id is required.");
        }

        var questions = await _context.Questions
            .Where(question => questionIds.Contains(question.QuestionId))
            .ToListAsync();

        EnsureQuestionsExist(questionIds, questions);
        EnsureQuestionsBelongToCollege(questions, assessment.CollegeId);
        EnsureQuestionsAreAvailableForCreate(questions);
        EnsureQuestionsMatchSubject(questions, subjectName);
        EnsureQuestionsMatchTopic(questions, topicName);

        return questions;
    }

    private static void EnsureQuestionsExist(IEnumerable<Guid> expectedQuestionIds, IReadOnlyCollection<Question> questions)
    {
        var missingQuestionIds = expectedQuestionIds.Except(questions.Select(question => question.QuestionId)).ToList();
        if (missingQuestionIds.Count > 0)
        {
            throw new KeyNotFoundException($"Question(s) not found: {string.Join(", ", missingQuestionIds)}.");
        }
    }

    private static void EnsureQuestionsBelongToCollege(IEnumerable<Question> questions, Guid collegeId)
    {
        var invalidQuestionIds = questions
            .Where(question => question.CollegeId != collegeId)
            .Select(question => question.QuestionId)
            .ToList();

        if (invalidQuestionIds.Count > 0)
        {
            throw new InvalidOperationException(
                $"Question(s) do not belong to this college: {string.Join(", ", invalidQuestionIds)}.");
        }
    }

    private static void EnsureQuestionsAreAvailableForCreate(IEnumerable<Question> questions)
    {
        var unavailableQuestionIds = questions
            .Where(question => !question.IsActive || question.AssessmentId.HasValue)
            .Select(question => question.QuestionId)
            .ToList();

        if (unavailableQuestionIds.Count > 0)
        {
            throw new InvalidOperationException(
                $"Question(s) are not available in the question bank: {string.Join(", ", unavailableQuestionIds)}.");
        }
    }

    private static void EnsureQuestionsMatchSubject(IEnumerable<Question> questions, string subjectName)
    {
        var mismatchedQuestionIds = questions
            .Where(question => !string.Equals(
                question.Subject?.Trim(),
                subjectName,
                StringComparison.OrdinalIgnoreCase))
            .Select(question => question.QuestionId)
            .ToList();

        if (mismatchedQuestionIds.Count > 0)
        {
            throw new InvalidOperationException(
                $"Question(s) do not belong to subject '{subjectName}': {string.Join(", ", mismatchedQuestionIds)}.");
        }
    }

    private static void EnsureQuestionsMatchTopic(IEnumerable<Question> questions, string topicName)
    {
        var mismatchedQuestionIds = questions
            .Where(question => !string.Equals(
                question.Topic?.Trim(),
                topicName,
                StringComparison.OrdinalIgnoreCase))
            .Select(question => question.QuestionId)
            .ToList();

        if (mismatchedQuestionIds.Count > 0)
        {
            throw new InvalidOperationException(
                $"Question(s) do not belong to topic '{topicName}': {string.Join(", ", mismatchedQuestionIds)}.");
        }
    }

    private static void PrepareAssessmentForCreation(Assessment assessment, IEnumerable<Question> questions)
    {
        assessment.AssessmentId = assessment.AssessmentId == Guid.Empty ? Guid.NewGuid() : assessment.AssessmentId;
        assessment.AssessmentType = ResolveAssessmentType(questions);
        assessment.DifficultyLevel = CalculateDifficultyLevel(questions);
        assessment.CreatedAt = DateTime.UtcNow;
    }

    private static void PrepareAssessmentForPublish(Assessment assessment, IEnumerable<Question> questions)
    {
        assessment.AssessmentType = ResolveAssessmentType(questions);
        assessment.DifficultyLevel = CalculateDifficultyLevel(questions);
        assessment.AssessmentStatus = AssessmentStatus.Scheduled;
        assessment.ModifiedAt = DateTime.UtcNow;
    }

    private static List<AssessmentQuestion> BuildAssessmentQuestions(
        Guid assessmentId,
        IReadOnlyCollection<Question> questions,
        IReadOnlyList<Guid> orderedQuestionIds)
    {
        var questionLookup = questions.ToDictionary(question => question.QuestionId);

        return orderedQuestionIds
            .Select((questionId, index) => new AssessmentQuestion
            {
                AssessmentId = assessmentId,
                QuestionId = questionId,
                DisplayOrder = index + 1,
                Marks = questionLookup[questionId].Marks
            })
            .ToList();
    }

    private static void AssignQuestionsToAssessment(
        IEnumerable<Question> questions,
        Guid assessmentId,
        bool updateOnlyWhenChanged = false)
    {
        var modifiedAt = DateTime.UtcNow;

        foreach (var question in questions)
        {
            if (updateOnlyWhenChanged && question.AssessmentId == assessmentId)
            {
                continue;
            }

            question.AssessmentId = assessmentId;
            question.ModifiedAt = modifiedAt;
        }
    }

    private async Task<Assessment> GetAssessmentByIdAsync(Guid assessmentId)
    {
        var assessment = await _context.Assessments
            .FirstOrDefaultAsync(item => item.AssessmentId == assessmentId);

        if (assessment is null)
        {
            throw new KeyNotFoundException($"Assessment '{assessmentId}' was not found.");
        }

        return assessment;
    }

    private static void EnsureDeleteAuthorized(Assessment assessment, DeleteAssessmentRequest request)
    {
        if (IsCollegeAdmin(request.RequesterRole))
        {
            if (!request.CollegeId.HasValue || request.CollegeId.Value == Guid.Empty)
            {
                throw new ArgumentException("CollegeId is required for college admin delete operations.");
            }

            if (assessment.CollegeId != request.CollegeId.Value)
            {
                throw new UnauthorizedAccessException("College admin can delete assessments only for its own college.");
            }

            return;
        }

        if (!IsSuperAdmin(request.RequesterRole))
        {
            throw new UnauthorizedAccessException("Only SuperAdmin and CollegeAdmin can delete assessments.");
        }
    }

    private static void EnsureAssessmentCanBePublished(Assessment assessment)
    {
        if (assessment.AssessmentStatus is not AssessmentStatus.Draft and not AssessmentStatus.Scheduled)
        {
            throw new InvalidOperationException("Only draft or scheduled assessments can be published.");
        }
    }

    private static void ValidateQuestionsForPublish(Assessment assessment, List<Question> questions)
    {
        if (questions.Count == 0)
        {
            throw new InvalidOperationException("At least one question must be linked to the assessment before publishing.");
        }

        EnsureQuestionsBelongToCollege(questions, assessment.CollegeId);

        var inactiveQuestionIds = questions
            .Where(question => !question.IsActive)
            .Select(question => question.QuestionId)
            .ToList();

        if (inactiveQuestionIds.Count > 0)
        {
            throw new InvalidOperationException(
                $"Question(s) are inactive and cannot be published: {string.Join(", ", inactiveQuestionIds)}.");
        }
    }

    private async Task<List<Question>> LoadQuestionsForPublishAsync(Assessment assessment)
    {
        if (assessment.AssessmentQuestions.Count > 0)
        {
            var questionIds = assessment.AssessmentQuestions
                .OrderBy(item => item.DisplayOrder)
                .Select(item => item.QuestionId)
                .ToList();

            var questions = await _context.Questions
                .Where(question => questionIds.Contains(question.QuestionId))
                .ToListAsync();

            EnsureQuestionsExist(questionIds, questions);

            var questionLookup = questions.ToDictionary(question => question.QuestionId);
            return questionIds.Select(questionId => questionLookup[questionId]).ToList();
        }

        return await _context.Questions
            .Where(question => question.AssessmentId == assessment.AssessmentId)
            .OrderBy(question => question.CreatedAt)
            .ThenBy(question => question.QuestionId)
            .ToListAsync();
    }

    private async Task<List<StudentAssessmentListItemRecord>> GetCompletedStudentAssessmentsAsync(Student student, Guid batchId)
    {
        var assessmentQuery = BuildStudentAssessmentQuery(student.CollegeId, batchId);

        var completedAttemptGroups = _context.Attempts
            .AsNoTracking()
            .Where(attempt =>
                attempt.StudentId == student.StudentId &&
                (attempt.AttemptStatus == AttemptStatus.Submitted ||
                 attempt.AttemptStatus == AttemptStatus.Auto_Submitted))
            .GroupBy(attempt => attempt.AssessmentId)
            .Select(group => new
            {
                AssessmentId = group.Key,
                LatestSubmittedAt = group.Max(item => item.SubmittedAt)
            });

        var completedAssessments = await (
            from attemptGroup in completedAttemptGroups
            join assessment in assessmentQuery
                on attemptGroup.AssessmentId equals assessment.AssessmentId
            orderby attemptGroup.LatestSubmittedAt descending, assessment.StartDateTime descending
            select assessment)
            .ToListAsync();

        return completedAssessments
            .Select(assessment => ToStudentAssessmentListItem(assessment, nameof(AssessmentStatus.Completed)))
            .ToList();
    }

    private async Task<List<StudentAssessmentListItemRecord>> GetActiveStudentAssessmentsAsync(
        Student student,
        Guid batchId,
        IReadOnlySet<string> normalizedStatuses)
    {
        var includeLive = normalizedStatuses.Contains(nameof(AssessmentStatus.Live));
        var includeScheduled = normalizedStatuses.Contains(nameof(AssessmentStatus.Scheduled));

        var completedAssessmentIds = await _context.Attempts
            .AsNoTracking()
            .Where(attempt =>
                attempt.StudentId == student.StudentId &&
                (attempt.AttemptStatus == AttemptStatus.Submitted ||
                 attempt.AttemptStatus == AttemptStatus.Auto_Submitted))
            .Select(attempt => attempt.AssessmentId)
            .Distinct()
            .ToListAsync();

        var assessments = await BuildStudentAssessmentQuery(student.CollegeId, batchId)
            .Where(assessment =>
                (includeLive && assessment.AssessmentStatus == AssessmentStatus.Live) ||
                (includeScheduled && assessment.AssessmentStatus == AssessmentStatus.Scheduled))
            .Where(assessment => !completedAssessmentIds.Contains(assessment.AssessmentId))
            .OrderBy(assessment => assessment.StartDateTime ?? DateTime.MaxValue)
            .ThenBy(assessment => assessment.AssessmentName)
            .ToListAsync();

        return assessments
            .Select(assessment => ToStudentAssessmentListItem(assessment, assessment.AssessmentStatus.ToString()))
            .ToList();
    }

    private IQueryable<Assessment> BuildStudentAssessmentQuery(Guid collegeId, Guid batchId)
    {
        return _context.Assessments
            .AsNoTracking()
            .Where(assessment =>
                assessment.CollegeId == collegeId &&
                assessment.AssessmentStatus != AssessmentStatus.Soft_Delete &&
                assessment.AssignedBatchIds.Contains(batchId));
    }

    private static StudentAssessmentListItemRecord ToStudentAssessmentListItem(Assessment assessment, string assessmentStatus)
    {
        return new StudentAssessmentListItemRecord(
            assessment.AssessmentId,
            assessment.AssessmentName,
            assessmentStatus.ToUpperInvariant(),
            assessment.DurationMinutes,
            assessment.TotalMarks,
            assessment.DifficultyLevel,
            assessment.StartDateTime,
            assessment.EndDateTime);
    }

    private async Task SaveChangesWithWrapAsync(string errorMessage)
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException(errorMessage, ex);
        }
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

    private static void ValidateDeleteAssessmentRequest(DeleteAssessmentRequest request)
    {
        if (request.AssessmentId == Guid.Empty)
        {
            throw new ArgumentException("AssessmentId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.DeletedBy))
        {
            throw new ArgumentException("DeletedBy is required.");
        }

        if (string.IsNullOrWhiteSpace(request.RequesterRole))
        {
            throw new ArgumentException("RequesterRole is required.");
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

    private static bool IsCollegeAdmin(string requesterRole)
        => string.Equals(requesterRole?.Trim(), "CollegeAdmin", StringComparison.OrdinalIgnoreCase);

    private static bool IsSuperAdmin(string requesterRole)
        => string.Equals(requesterRole?.Trim(), "SuperAdmin", StringComparison.OrdinalIgnoreCase);

    private static int CalculateDifficultyLevel(IEnumerable<Question> questions)
    {
        var roundedAverage = (int)Math.Round(
            questions.Average(question => question.DifficultyLevel),
            MidpointRounding.AwayFromZero);

        return Math.Max(1, roundedAverage);
    }

    private static HashSet<string> NormalizeStudentAssessmentStatuses(IReadOnlyCollection<string> assessmentStatuses)
    {
        var normalizedStatuses = assessmentStatuses
            .Where(status => !string.IsNullOrWhiteSpace(status))
            .Select(status => status.Trim().Replace(" ", "_"))
            .Select(status => status.Equals("LIVE", StringComparison.OrdinalIgnoreCase) ? nameof(AssessmentStatus.Live) : status)
            .Select(status => status.Equals("SCHEDULED", StringComparison.OrdinalIgnoreCase) ? nameof(AssessmentStatus.Scheduled) : status)
            .Select(status => status.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase) ? nameof(AssessmentStatus.Completed) : status)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (normalizedStatuses.Count == 0)
        {
            throw new ArgumentException("At least one assessment status filter is required.");
        }

        var allowedStatuses = new HashSet<string>(
            [nameof(AssessmentStatus.Live), nameof(AssessmentStatus.Scheduled), nameof(AssessmentStatus.Completed)],
            StringComparer.OrdinalIgnoreCase);

        var invalidStatuses = normalizedStatuses
            .Where(status => !allowedStatuses.Contains(status))
            .ToArray();

        if (invalidStatuses.Length > 0)
        {
            throw new ArgumentException(
                $"Unsupported assessment status filter(s): {string.Join(", ", invalidStatuses)}.");
        }

        if (normalizedStatuses.Contains(nameof(AssessmentStatus.Completed)) && normalizedStatuses.Count > 1)
        {
            throw new ArgumentException("Completed cannot be combined with Live or Scheduled filters.");
        }

        return normalizedStatuses;
    }
}
