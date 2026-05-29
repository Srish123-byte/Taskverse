using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Taskverse.API.Assessments.Service.Mappings;
using Taskverse.API.Assessments.Service.Models;
using Taskverse.API.Assessments.Service.Services;
using Taskverse.Data.Enums;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.Assessments.Service.Managers;

public class AssessmentManager : IAssessmentManager
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 10;
    private const int MaximumPageSize = 100;

    private readonly TaskverseContext _context;
    private readonly AssessmentSettings _assessmentSettings;
    private readonly IStudentAttemptAnswerSaveStrategyFactory _studentAttemptAnswerSaveStrategyFactory;
    private readonly IReportsServiceClient _reportsServiceClient;
    private readonly ILogger<AssessmentManager> _logger;

    public AssessmentManager(
        TaskverseContext context,
        IOptions<AssessmentSettings> assessmentSettings,
        IStudentAttemptAnswerSaveStrategyFactory studentAttemptAnswerSaveStrategyFactory,
        IReportsServiceClient reportsServiceClient,
        ILogger<AssessmentManager> logger)
    {
        _context = context;
        _assessmentSettings = assessmentSettings.Value;
        _studentAttemptAnswerSaveStrategyFactory = studentAttemptAnswerSaveStrategyFactory;
        _reportsServiceClient = reportsServiceClient;
        _logger = logger;
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

    public async Task<AssessmentSubjectTopicCatalogRecord> GetSubjectTopicCatalog(AssessmentAccessibleBatchesRequest request)
    {
        ValidateAccessibleBatchesRequest(request);

        var accessibleBatchIds = await BuildAccessibleBatchQuery(request)
            .Select(batch => batch.BatchId)
            .ToListAsync();

        if (accessibleBatchIds.Count == 0)
        {
            return new AssessmentSubjectTopicCatalogRecord([]);
        }

        var subjectBatchLinks = await _context.SubjectBatches
            .AsNoTracking()
            .Where(link => accessibleBatchIds.Contains(link.BatchId))
            .Include(link => link.Subject)
                .ThenInclude(subject => subject.Topics.Where(topic => topic.IsActive))
            .ToListAsync();

        var subjects = subjectBatchLinks
            .Where(link => link.Subject.IsActive)
            .GroupBy(link => new { link.Subject.SubjectId, link.Subject.SubjectName })
            .Select(subjectGroup =>
            {
                var batchIds = subjectGroup
                    .Select(link => link.BatchId)
                    .Distinct()
                    .OrderBy(batchId => batchId)
                    .ToArray();

                var topics = subjectGroup
                    .SelectMany(link => link.Subject.Topics.Select(topic => new { link.BatchId, Topic = topic }))
                    .GroupBy(item => new { item.Topic.TopicId, item.Topic.TopicName })
                    .Select(topicGroup => new AssessmentTopicCatalogRecord(
                        topicGroup.Key.TopicId,
                        topicGroup.Key.TopicName,
                        topicGroup.Select(item => item.BatchId)
                            .Distinct()
                            .OrderBy(batchId => batchId)
                            .ToArray()))
                    .OrderBy(item => item.TopicName)
                    .ToList();

                return new AssessmentSubjectCatalogRecord(
                    subjectGroup.Key.SubjectId,
                    subjectGroup.Key.SubjectName,
                    batchIds,
                    topics);
            })
            .OrderBy(item => item.SubjectName)
            .ToList();

        return new AssessmentSubjectTopicCatalogRecord(subjects);
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
        ValidateStudentAttemptRequest(assessmentId, studentUserId);

        var student = await GetStudentByUserIdAsync(studentUserId);
        var assessment = await GetStudentAssessmentForAttemptAsync(assessmentId, student);

        var latestAttempt = await GetLatestAttemptAsync(student.StudentId, assessmentId);
        if (latestAttempt is not null)
        {
            if (latestAttempt.AttemptStatus is AttemptStatus.Submitted or AttemptStatus.Auto_Submitted)
            {
                throw new InvalidOperationException("This assessment has already been submitted by the current student.");
            }

            if (await EnsureAttemptClosedIfExpiredAsync(latestAttempt, assessment))
            {
                throw new InvalidOperationException("The previous attempt has already expired and was auto-submitted.");
            }

            throw new InvalidOperationException(
                $"An active attempt already exists for this assessment. Recover it using attempt id '{latestAttempt.AttemptId}'.");
        }

        var attempt = CreateInProgressAttempt(student.StudentId, assessment);
        _context.Attempts.Add(attempt);

        try
        {
            await SaveChangesWithWrapAsync("Unable to start the assessment attempt.");
        }
        catch (InvalidOperationException ex) when (IsDuplicateStudentAttempt(ex))
        {
            var existingAttempt = await GetLatestAttemptAsync(student.StudentId, assessmentId);
            if (existingAttempt is not null)
            {
                if (existingAttempt.AttemptStatus is AttemptStatus.Submitted or AttemptStatus.Auto_Submitted)
                {
                    throw new InvalidOperationException(
                        "This assessment has already been submitted by the current student.");
                }

                throw new InvalidOperationException(
                    $"An active attempt already exists for this assessment. Recover it using attempt id '{existingAttempt.AttemptId}'.");
            }

            throw;
        }

        return attempt.ToStudentAssessmentStartRecord();
    }

    public async Task<StudentAttemptRecoveryRecord> GetStudentAttemptRecovery(Guid attemptId, Guid studentUserId)
    {
        if (attemptId == Guid.Empty)
        {
            throw new ArgumentException("Attempt id is required.");
        }

        ValidateStudentAttemptRequest(Guid.Empty, studentUserId, validateAssessmentId: false);

        var student = await GetStudentByUserIdAsync(studentUserId);
        var attempt = await GetAttemptForStudentAsync(attemptId, student.StudentId);
        var assessment = await GetAssessmentForAttemptRecoveryAsync(attempt.AssessmentId, student);

        await EnsureAttemptClosedIfExpiredAsync(attempt, assessment);

        if (attempt.AttemptStatus is AttemptStatus.Submitted or AttemptStatus.Auto_Submitted)
        {
            attempt = await GetAttemptForStudentAsync(attemptId, student.StudentId);
        }

        return await BuildStudentAttemptRecoveryAsync(attempt, assessment);
    }

    public async Task<StudentAttemptAnswerRecord> SaveStudentAttemptAnswer(
        Guid attemptId,
        Guid questionId,
        Guid studentUserId,
        SaveStudentAttemptAnswerRequest request)
    {
        if (attemptId == Guid.Empty)
        {
            throw new ArgumentException("Attempt id is required.");
        }

        ValidateStudentAttemptRequest(Guid.Empty, studentUserId, validateAssessmentId: false);

        if (request is null)
        {
            throw new ArgumentException("Attempt answer request is required.");
        }

        if (questionId == Guid.Empty)
        {
            throw new ArgumentException("Question id is required.");
        }

        var student = await GetStudentByUserIdAsync(studentUserId);
        var attempt = await GetAttemptForStudentAsync(attemptId, student.StudentId);
        var assessment = await GetAssessmentForAttemptRecoveryAsync(attempt.AssessmentId, student);

        if (await EnsureAttemptClosedIfExpiredAsync(attempt, assessment))
        {
            throw new InvalidOperationException("The assessment attempt has expired and was auto-submitted.");
        }

        if (attempt.AttemptStatus is not AttemptStatus.In_Progress)
        {
            throw new InvalidOperationException("Answers can only be saved for an in-progress attempt.");
        }

        var assessmentQuestion = assessment.AssessmentQuestions
            .FirstOrDefault(item => item.QuestionId == questionId);

        if (assessmentQuestion is null)
        {
            throw new KeyNotFoundException($"Question '{questionId}' was not found in this assessment attempt.");
        }

        var question = await _context.Questions
            .FirstOrDefaultAsync(item => item.QuestionId == questionId);

        if (question is null)
        {
            throw new KeyNotFoundException($"Question '{questionId}' was not found.");
        }

        var answeredAt = DateTime.UtcNow;
        var strategy = _studentAttemptAnswerSaveStrategyFactory.Resolve(question.QuestionType);
        var savedAnswer = await strategy.SaveAsync(_context, attempt, question, request, answeredAt);

        attempt.QuestionId = questionId;
        attempt.LastActivityAt = answeredAt;
        await RefreshAttemptProgressAsync(attempt);

        await SaveChangesWithWrapAsync("Unable to save the assessment answer.");

        return savedAnswer.ToStudentAttemptAnswerRecord();
    }

    public async Task<StudentAttemptSubmitRecord> SubmitStudentAttempt(Guid attemptId, Guid studentUserId)
    {
        if (attemptId == Guid.Empty)
        {
            throw new ArgumentException("Attempt id is required.");
        }

        ValidateStudentAttemptRequest(Guid.Empty, studentUserId, validateAssessmentId: false);

        var student = await GetStudentByUserIdAsync(studentUserId);
        var attempt = await GetAttemptForStudentAsync(attemptId, student.StudentId);
        var assessment = await GetAssessmentForAttemptRecoveryAsync(attempt.AssessmentId, student);

        if (attempt.AttemptStatus is AttemptStatus.Submitted or AttemptStatus.Auto_Submitted)
        {
            throw new InvalidOperationException("This assessment attempt has already been submitted.");
        }

        var submittedAt = DateTime.UtcNow;
        var isExpired = IsAttemptExpired(attempt, assessment, out var expiresAt);
        var finalStatus = isExpired ? AttemptStatus.Auto_Submitted : AttemptStatus.Submitted;
        var effectiveSubmittedAt = isExpired ? expiresAt : submittedAt;
        var effectiveExpiresAt = isExpired
            ? expiresAt
            : attempt.ExpiresAt
              ?? assessment.EndDateTime
              ?? attempt.StartedAt?.AddMinutes(assessment.DurationMinutes)
              ?? submittedAt;

        var finalizedAttempt = await FinalizeAttemptAsync(attempt, effectiveSubmittedAt, effectiveExpiresAt, finalStatus);
        return finalizedAttempt.ToStudentAttemptSubmitRecord();
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

    private IQueryable<Batch> BuildAccessibleBatchQuery(AssessmentAccessibleBatchesRequest request)
    {
        var normalizedRole = request.RequesterRole.Trim();

        var query = _context.Batches
            .AsNoTracking()
            .Include(batch => batch.Class)
            .Include(batch => batch.SubjectBatches)
            .Where(batch => batch.CollegeId == request.CollegeId);

        if (!string.Equals(normalizedRole, "Trainer", StringComparison.OrdinalIgnoreCase))
        {
            return query;
        }

        var trainerId = _context.Trainers
            .AsNoTracking()
            .Where(trainer =>
                trainer.UserId == request.RequesterUserId &&
                trainer.CollegeId == request.CollegeId)
            .Select(trainer => trainer.TrainerId);

        return query.Where(batch => batch.TrainerBatches.Any(link => trainerId.Contains(link.TrainerId)));
    }

    private static void ValidateAccessibleBatchesRequest(AssessmentAccessibleBatchesRequest request)
    {
        if (request is null)
        {
            throw new ArgumentException("Assessment bootstrap request is required.");
        }

        if (request.CollegeId == Guid.Empty)
        {
            throw new ArgumentException("CollegeId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.RequesterRole))
        {
            throw new ArgumentException("Requester role is required.");
        }

        if (string.Equals(request.RequesterRole.Trim(), "Trainer", StringComparison.OrdinalIgnoreCase) &&
            (!request.RequesterUserId.HasValue || request.RequesterUserId.Value == Guid.Empty))
        {
            throw new ArgumentException("Requester user id is required for trainer assessment bootstrap requests.");
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

    private static void ValidateStudentAttemptRequest(
        Guid assessmentId,
        Guid studentUserId,
        bool validateAssessmentId = true)
    {
        if (validateAssessmentId && assessmentId == Guid.Empty)
        {
            throw new ArgumentException("Assessment id is required.");
        }

        if (studentUserId == Guid.Empty)
        {
            throw new ArgumentException("Student user id is required.");
        }
    }

    private async Task<Student> GetStudentByUserIdAsync(Guid studentUserId)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(item => item.UserId == studentUserId);

        if (student is null)
        {
            throw new KeyNotFoundException($"Student profile was not found for user '{studentUserId}'.");
        }

        return student;
    }

    private async Task<Assessment> GetStudentAssessmentForAttemptAsync(Guid assessmentId, Student student)
    {
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

        return assessment;
    }

    private async Task<Assessment> GetAssessmentForAttemptRecoveryAsync(Guid assessmentId, Student student)
    {
        if (!student.BatchId.HasValue || student.BatchId.Value == Guid.Empty)
        {
            throw new KeyNotFoundException($"Attempt for assessment '{assessmentId}' was not found for the current student.");
        }

        var assessment = await BuildStudentAssessmentQuery(student.CollegeId, student.BatchId.Value)
            .Include(item => item.AssessmentQuestions)
            .FirstOrDefaultAsync(item => item.AssessmentId == assessmentId);

        if (assessment is null)
        {
            throw new KeyNotFoundException($"Attempt for assessment '{assessmentId}' was not found for the current student.");
        }

        return assessment;
    }

    private async Task<Attempt?> GetLatestAttemptAsync(Guid studentId, Guid assessmentId)
    {
        return await _context.Attempts
            .Where(item => item.AssessmentId == assessmentId && item.StudentId == studentId)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync();
    }

    private async Task<Attempt> GetAttemptForStudentAsync(Guid attemptId, Guid studentId)
    {
        var attempt = await _context.Attempts
            .FirstOrDefaultAsync(item => item.AttemptId == attemptId && item.StudentId == studentId);

        if (attempt is null)
        {
            throw new KeyNotFoundException($"Attempt '{attemptId}' was not found for the current student.");
        }

        return attempt;
    }

    private Attempt CreateInProgressAttempt(Guid studentId, Assessment assessment)
    {
        var startedAt = DateTime.UtcNow;
        var totalQuestions = assessment.AssessmentQuestions.Count;
        var expiresAt = assessment.EndDateTime ?? startedAt.AddMinutes(assessment.DurationMinutes);

        return new Attempt
        {
            AttemptId = Guid.NewGuid(),
            AssessmentId = assessment.AssessmentId,
            StudentId = studentId,
            StartedAt = startedAt,
            LastActivityAt = startedAt,
            ExpiresAt = expiresAt,
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
    }

    private async Task<bool> EnsureAttemptClosedIfExpiredAsync(Attempt attempt, Assessment assessment)
    {
        if (attempt.AttemptStatus is not AttemptStatus.In_Progress)
        {
            return false;
        }

        if (!IsAttemptExpired(attempt, assessment, out var expiresAt))
        {
            return false;
        }

        await AutoSubmitAttemptAsync(attempt, expiresAt);
        return true;
    }

    private static bool IsAttemptExpired(Attempt attempt, Assessment assessment, out DateTime expiresAt)
    {
        expiresAt = attempt.ExpiresAt
            ?? assessment.EndDateTime
            ?? attempt.StartedAt?.AddMinutes(assessment.DurationMinutes)
            ?? DateTime.UtcNow;

        return DateTime.UtcNow >= expiresAt;
    }

    private async Task AutoSubmitAttemptAsync(Attempt attempt, DateTime expiresAt)
    {
        await FinalizeAttemptAsync(attempt, expiresAt, expiresAt, AttemptStatus.Auto_Submitted);
    }

    private async Task RefreshAttemptProgressAsync(Attempt attempt)
    {
        var attemptedQuestions = await GetAttemptedQuestionCountAsync(attempt.AttemptId);

        attempt.AttemptedQuestions = attemptedQuestions;
        attempt.UnansweredQuestions = Math.Max(0, attempt.TotalQuestions - attemptedQuestions);
    }

    private async Task<StudentAttemptRecoveryRecord> BuildStudentAttemptRecoveryAsync(Attempt attempt, Assessment assessment)
    {
        var orderedAssessmentQuestions = assessment.AssessmentQuestions
            .OrderBy(item => item.DisplayOrder)
            .ToList();

        var questionIds = orderedAssessmentQuestions
            .Select(item => item.QuestionId)
            .ToList();

        var questions = await _context.Questions
            .AsNoTracking()
            .Where(item => questionIds.Contains(item.QuestionId))
            .ToDictionaryAsync(item => item.QuestionId);

        var attemptAnswers = await _context.AttemptAnswers
            .AsNoTracking()
            .Where(item => item.AttemptId == attempt.AttemptId)
            .ToDictionaryAsync(item => item.QuestionId);

        var questionRecords = orderedAssessmentQuestions
            .Where(item => questions.ContainsKey(item.QuestionId))
            .Select(item => questions[item.QuestionId].ToStudentAttemptRecoveryQuestionRecord(
                item.DisplayOrder,
                attemptAnswers.GetValueOrDefault(item.QuestionId)))
            .ToList();

        var expiresAt = attempt.ExpiresAt
            ?? assessment.EndDateTime
            ?? attempt.StartedAt?.AddMinutes(assessment.DurationMinutes)
            ?? DateTime.UtcNow;

        attempt.ExpiresAt = expiresAt;

        var remainingSeconds = attempt.AttemptStatus == AttemptStatus.In_Progress
            ? Math.Max(0, (int)Math.Ceiling((expiresAt - DateTime.UtcNow).TotalSeconds))
            : 0;

        return attempt.ToStudentAttemptRecoveryRecord(assessment, remainingSeconds, questionRecords);
    }

    private static int CalculateTimeTakenSeconds(DateTime? startedAt, DateTime endedAt)
    {
        if (!startedAt.HasValue)
        {
            return 0;
        }

        return Math.Max(0, (int)Math.Round((endedAt - startedAt.Value).TotalSeconds, MidpointRounding.AwayFromZero));
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

    private async Task<Attempt> FinalizeAttemptAsync(
        Attempt attempt,
        DateTime submittedAt,
        DateTime expiresAt,
        AttemptStatus finalStatus)
    {
        var attemptedQuestions = await GetAttemptedQuestionCountAsync(attempt.AttemptId);
        var unansweredQuestions = Math.Max(0, attempt.TotalQuestions - attemptedQuestions);
        var timeTakenSeconds = CalculateTimeTakenSeconds(attempt.StartedAt, submittedAt);

        try
        {
            var affectedRows = await _context.Attempts
                .Where(item =>
                    item.AttemptId == attempt.AttemptId &&
                    item.StudentId == attempt.StudentId &&
                    item.AttemptStatus == AttemptStatus.In_Progress)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.AttemptStatus, finalStatus)
                    .SetProperty(item => item.SubmittedAt, submittedAt)
                    .SetProperty(item => item.LastActivityAt, submittedAt)
                    .SetProperty(item => item.ExpiresAt, expiresAt)
                    .SetProperty(item => item.TimeTakenSeconds, timeTakenSeconds)
                    .SetProperty(item => item.AttemptedQuestions, attemptedQuestions)
                    .SetProperty(item => item.UnansweredQuestions, unansweredQuestions));

            if (affectedRows == 0)
            {
                var currentAttempt = await _context.Attempts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item =>
                        item.AttemptId == attempt.AttemptId &&
                        item.StudentId == attempt.StudentId);

                if (currentAttempt is not null &&
                    currentAttempt.AttemptStatus is AttemptStatus.Submitted or AttemptStatus.Auto_Submitted)
                {
                    throw new InvalidOperationException("This assessment attempt has already been submitted.");
                }

                throw new InvalidOperationException("This assessment attempt could not be submitted.");
            }

            await _context.Entry(attempt).ReloadAsync();
            await TryEvaluateAttemptResultAsync(attempt.AttemptId);
            return attempt;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Unable to submit the assessment attempt.", ex);
        }
    }

    private async Task<int> GetAttemptedQuestionCountAsync(Guid attemptId)
    {
        return await _context.AttemptAnswers
            .Where(item =>
                item.AttemptId == attemptId &&
                item.SelectedAnswer != null &&
                item.SelectedAnswer != string.Empty)
            .Select(item => item.QuestionId)
            .Distinct()
            .CountAsync();
    }

    private async Task TryEvaluateAttemptResultAsync(Guid attemptId)
    {
        try
        {
            await _reportsServiceClient.EvaluateAttemptAsync(attemptId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Attempt {AttemptId} was finalized but result evaluation in Reports service did not complete.",
                attemptId);
        }
    }

    private static bool IsDuplicateStudentAttempt(InvalidOperationException exception)
    {
        return exception.InnerException is DbUpdateException dbUpdateException &&
               dbUpdateException.InnerException is PostgresException postgresException &&
               postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
               string.Equals(postgresException.ConstraintName, "ux_attempts_assessment_student", StringComparison.Ordinal);
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
