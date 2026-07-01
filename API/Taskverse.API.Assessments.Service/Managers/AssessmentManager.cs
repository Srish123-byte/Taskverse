using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Taskverse.API.Assessments.Service.Clients;
using Taskverse.API.Assessments.Service.Mappings;
using Taskverse.API.Assessments.Service.Models;
using Taskverse.API.Assessments.Service.Services;
using Taskverse.Data.Enums;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.Assessments.Service.Managers;

/// <summary>
/// Coordinates assessment persistence, validation, authorization, and student attempt workflows.
/// </summary>
public class AssessmentManager : IAssessmentManager
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 10;
    private const int MaximumPageSize = 500;
    private static readonly TimeSpan PartialDraftStartOffset = TimeSpan.FromHours(1);
    private static readonly TimeSpan PartialDraftMinimumDuration = TimeSpan.FromMinutes(1);

    private readonly TaskverseContext _context;
    private readonly AssessmentSettings _assessmentSettings;
    private readonly IStudentAttemptAnswerSaveStrategyFactory _studentAttemptAnswerSaveStrategyFactory;
    private readonly IReportsServiceClient _reportsServiceClient;
    private readonly IProctorServiceClient _proctorServiceClient;
    private readonly ILogger<AssessmentManager> _logger;

    private sealed record ResultProcessingContext(int PassingPercentage, bool ShowResultsImmediately);

    public AssessmentManager(
        TaskverseContext context,
        IOptions<AssessmentSettings> assessmentSettings,
        IStudentAttemptAnswerSaveStrategyFactory studentAttemptAnswerSaveStrategyFactory,
        IReportsServiceClient reportsServiceClient,
        IProctorServiceClient proctorServiceClient,
        ILogger<AssessmentManager> logger)
    {
        _context = context;
        _assessmentSettings = assessmentSettings.Value;
        _studentAttemptAnswerSaveStrategyFactory = studentAttemptAnswerSaveStrategyFactory;
        _reportsServiceClient = reportsServiceClient;
        _proctorServiceClient = proctorServiceClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Assessment> CreateAssessment(Assessment assessment, List<Guid> questionIds)
        => await ExecuteDbOperationAsync(
            () => CreateAssessmentInternalAsync(assessment, questionIds, AssessmentStatus.Draft, allowPartialDraft: true),
            "creating the assessment");

    /// <inheritdoc />
    public async Task<Assessment> GetAssessment(Guid assessmentId, Guid collegeId, string requesterRole, string requesterName)
    {
        if (collegeId == Guid.Empty)
        {
            throw new ArgumentException("CollegeId is required.");
        }

        if (string.IsNullOrWhiteSpace(requesterRole))
        {
            throw new ArgumentException("RequesterRole is required.");
        }

        if (string.IsNullOrWhiteSpace(requesterName))
        {
            throw new ArgumentException("RequesterName is required.");
        }

        return await ExecuteDbOperationAsync(async () =>
        {
            var assessment = await GetAssessmentForUpdateAsync(assessmentId);
            EnsureAssessmentReadAuthorized(assessment, collegeId, requesterRole, requesterName);
            await PopulateAssessmentLabelsAsync([assessment]);
            return assessment;
        }, "retrieving the assessment");
    }

    /// <inheritdoc />
    public async Task<Assessment> UpdateAssessment(Guid assessmentId, UpdateAssessmentRequest request)
    {
        ValidateUpdateAssessmentRequest(request);

        return await ExecuteDbOperationAsync(async () =>
        {
            var assessment = await GetAssessmentForUpdateAsync(assessmentId);
            EnsureAssessmentCanBeUpdated(assessment);
            EnsureUpdateAuthorized(assessment, request);

            if (request.IsDraftSave)
            {
                return await UpdateAssessmentDraftAsync(assessment, request);
            }

            var updatedAssessment = request.ToEntity(_assessmentSettings, assessment.AssessmentStatus);
            updatedAssessment.ShowResultsImmediately = assessment.ShowResultsImmediately;
            updatedAssessment.IsTotalMarksAutoCalculated = assessment.IsTotalMarksAutoCalculated;
            updatedAssessment.CreatedBy = assessment.CreatedBy;
            updatedAssessment.CreatedAt = assessment.CreatedAt;
            updatedAssessment.ModifiedAt = DateTime.UtcNow;

            ValidateAssessment(updatedAssessment, request.QuestionIds);

            var normalizedQuestionIds = request.QuestionIds.NormalizeQuestionIds();
            var normalizedAssignedBatchIds = NormalizeAssignedBatchIds(updatedAssessment.AssignedBatchIds);

            await ValidateAssignedBatchesAsync(updatedAssessment.CollegeId, normalizedAssignedBatchIds);

            updatedAssessment.AssignedBatchIds = normalizedAssignedBatchIds;

            var questions = await LoadAndValidateQuestionsForCreateAsync(
                updatedAssessment,
                normalizedQuestionIds);

            ApplyAutoCalculatedTotalMarksIfEnabled(updatedAssessment, questions);
            ValidateQuestionBudget(updatedAssessment, questions);
            await PopulateAssessmentClassificationAsync(updatedAssessment, questions);

            ApplyAssessmentUpdates(assessment, updatedAssessment, questions);
            await SyncAssessmentQuestionsAsync(assessment, questions, normalizedQuestionIds);

            await SaveChangesWithWrapAsync("Unable to update the assessment.");

            return assessment;
        }, "updating the assessment");
    }

    private async Task<Assessment> UpdateAssessmentDraftAsync(Assessment assessment, UpdateAssessmentRequest request)
    {
        var updatedAssessment = request.ToEntity(_assessmentSettings, AssessmentStatus.Draft);
        updatedAssessment.ShowResultsImmediately = assessment.ShowResultsImmediately;
        updatedAssessment.IsTotalMarksAutoCalculated = assessment.IsTotalMarksAutoCalculated;
        updatedAssessment.CreatedBy = assessment.CreatedBy;
        updatedAssessment.CreatedAt = assessment.CreatedAt;
        updatedAssessment.ModifiedAt = DateTime.UtcNow;

        var normalizedQuestionIds = (request.QuestionIds ?? []).NormalizeQuestionIds();
        var normalizedAssignedBatchIds = NormalizeAssignedBatchIds(updatedAssessment.AssignedBatchIds);
        await ValidateAssignedBatchesAsync(updatedAssessment.CollegeId, normalizedAssignedBatchIds);
        updatedAssessment.AssignedBatchIds = normalizedAssignedBatchIds;

        var questions = await LoadQuestionsForDraftUpdateAsync(updatedAssessment.CollegeId, normalizedQuestionIds);
        ApplyPartialDraftRequiredDefaults(updatedAssessment, DateTime.UtcNow);
        await PopulateAssessmentClassificationAsync(updatedAssessment, questions);

        ApplyPartialDraftUpdates(assessment, updatedAssessment);
        await SyncAssessmentQuestionsAsync(assessment, questions, normalizedQuestionIds);

        await SaveChangesWithWrapAsync("Unable to save the assessment draft.");

        return assessment;
    }

    /// <inheritdoc />
    public async Task<Assessment> ScheduleAssessment(Assessment assessment, List<Guid> questionIds)
        => await ExecuteDbOperationAsync(
            () => CreateAssessmentInternalAsync(assessment, questionIds, AssessmentStatus.Scheduled),
            "scheduling the assessment");

    private async Task<Assessment> CreateAssessmentInternalAsync(
        Assessment assessment,
        List<Guid> questionIds,
        AssessmentStatus targetStatus,
        bool allowPartialDraft = false)
    {
        if (allowPartialDraft)
        {
            return await CreatePartialDraftAsync(assessment, questionIds.NormalizeQuestionIds());
        }

        ValidateAssessment(assessment, questionIds);

        var normalizedQuestionIds = questionIds.NormalizeQuestionIds();
        var normalizedAssignedBatchIds = NormalizeAssignedBatchIds(assessment.AssignedBatchIds);

        await ValidateAssignedBatchesAsync(assessment.CollegeId, normalizedAssignedBatchIds);

        assessment.AssignedBatchIds = normalizedAssignedBatchIds;

        var questions = await LoadAndValidateQuestionsForCreateAsync(
            assessment,
            normalizedQuestionIds);

        ApplyAutoCalculatedTotalMarksIfEnabled(assessment, questions);
        ValidateQuestionBudget(assessment, questions);
        await PopulateAssessmentClassificationAsync(assessment, questions);

        PrepareAssessmentForCreation(assessment, questions, targetStatus);
        assessment.AssessmentQuestions = BuildAssessmentQuestions(assessment.AssessmentId, questions, normalizedQuestionIds);

        _context.Assessments.Add(assessment);
        await SaveChangesWithWrapAsync("Unable to save the assessment.");

        return assessment;
    }

    private async Task<Assessment> CreatePartialDraftAsync(Assessment assessment, IReadOnlyList<Guid> questionIds)
    {
        ValidatePartialDraft(assessment);

        var now = DateTime.UtcNow;
        var questions = await LoadQuestionsForDraftUpdateAsync(assessment.CollegeId, questionIds);
        ApplyPartialDraftRequiredDefaults(assessment, now);
        await PopulateAssessmentClassificationAsync(assessment, questions);
        assessment.AssessmentId = assessment.AssessmentId == Guid.Empty ? Guid.NewGuid() : assessment.AssessmentId;
        assessment.AssessmentType = AssessmentType.Objective;
        assessment.AssessmentStatus = AssessmentStatus.Draft;
        assessment.DifficultyLevel = 1;
        assessment.AssignedBatchIds = NormalizeAssignedBatchIds(assessment.AssignedBatchIds);
        assessment.CreatedAt = now;
        assessment.ModifiedAt = now;

        _context.Assessments.Add(assessment);
        await SaveChangesWithWrapAsync("Unable to save the assessment draft.");

        if (questionIds.Count > 0)
        {
            await SyncAssessmentQuestionsAsync(assessment, questions, questionIds);
            await SaveChangesWithWrapAsync("Unable to save the assessment draft questions.");
        }

        return assessment;
    }

    private static void ApplyPartialDraftRequiredDefaults(Assessment assessment, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(assessment.AssessmentName))
        {
            assessment.AssessmentName = "Untitled draft";
        }

        if (assessment.DurationMinutes <= 0)
        {
            assessment.DurationMinutes = 1;
        }

        if (assessment.TotalMarks < 0)
        {
            assessment.TotalMarks = 0;
        }

        if (assessment.PassingPercentage < 0)
        {
            assessment.PassingPercentage = 0;
        }
        else if (assessment.PassingPercentage > 100)
        {
            assessment.PassingPercentage = 100;
        }

        assessment.AssignedBatchIds = NormalizeAssignedBatchIds(assessment.AssignedBatchIds);

        var safeStartDateTime = assessment.StartDateTime ?? utcNow.Add(PartialDraftStartOffset);
        if (safeStartDateTime <= utcNow)
        {
            safeStartDateTime = utcNow.Add(PartialDraftStartOffset);
        }

        assessment.StartDateTime = safeStartDateTime;

        var minimumEndDateTime = safeStartDateTime.Add(PartialDraftMinimumDuration);
        if (!assessment.EndDateTime.HasValue || assessment.EndDateTime.Value <= safeStartDateTime)
        {
            assessment.EndDateTime = minimumEndDateTime;
        }
    }

    private async Task<List<Question>> LoadQuestionsForDraftUpdateAsync(Guid collegeId, IReadOnlyList<Guid> questionIds)
    {
        if (questionIds.Count == 0)
        {
            return [];
        }

        var questions = await _context.Questions
            .Where(question => questionIds.Contains(question.QuestionId))
            .ToListAsync();

        EnsureQuestionsExist(questionIds, questions);
        EnsureQuestionsBelongToCollege(questions, collegeId);

        return questions;
    }

    /// <inheritdoc />
    public async Task DeleteAssessment(Guid assessmentId, DeleteAssessmentRequest request)
    {
        ValidateDeleteAssessmentRequest(request);

        await ExecuteDbOperationAsync(async () =>
        {
            var assessment = await GetAssessmentByIdAsync(assessmentId);
            EnsureAssessmentCanBeDeleted(assessment);
            EnsureDeleteAuthorized(assessment, request);

            var deletedAt = DateTime.UtcNow;

            assessment.IsDeleted = request.IsDeleted ?? true;
            assessment.AssessmentStatus = AssessmentStatus.Soft_Deleted;
            assessment.SoftDeletedAt = deletedAt;
            assessment.SoftDeletedBy = request.DeletedBy.Trim();
            assessment.ModifiedAt = deletedAt;

            await SaveChangesWithWrapAsync("Unable to delete the assessment.");
        }, "deleting the assessment");
    }

    /// <inheritdoc />
    public async Task<Assessment> PublishAssessment(Guid assessmentId)
    {
        return await ExecuteDbOperationAsync(async () =>
        {
            var assessment = await _context.Assessments
                .Include(item => item.AssessmentQuestions)
                .FirstOrDefaultAsync(item => item.AssessmentId == assessmentId);

            if (assessment is null)
            {
                throw new KeyNotFoundException($"Assessment '{assessmentId}' was not found.");
            }

            EnsureAssessmentCanBePublished(assessment);

            var questions = await LoadQuestionsForPublishAsync(assessment);
            ValidateQuestionsForPublish(assessment, questions);
            ApplyAutoCalculatedTotalMarksIfEnabled(assessment, questions);
            ValidateQuestionBudget(assessment, questions);
            await PopulateAssessmentClassificationAsync(assessment, questions);

            PrepareAssessmentForPublish(assessment, questions);

            if (assessment.AssessmentQuestions.Count == 0)
            {
                assessment.AssessmentQuestions = BuildAssessmentQuestions(
                    assessment.AssessmentId,
                    questions,
                    questions.Select(question => question.QuestionId).ToList());
            }

            await SaveChangesWithWrapAsync("Unable to publish the assessment.");

            return assessment;
        }, "publishing the assessment");
    }

    /// <inheritdoc />
    /// <inheritdoc />
    public async Task<AssessmentAssignmentCatalogRecord> GetTrainerAssignedClassesAndBatches(AssessmentAccessibleBatchesRequest request)
    {
        ValidateTrainerAssignmentRequest(request);

        return await ExecuteDbOperationAsync(async () =>
        {
            var trainer = await _context.Trainers
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.UserId == request.RequesterUserId!.Value &&
                    item.CollegeId == request.CollegeId);

            if (trainer is null)
            {
                return new AssessmentAssignmentCatalogRecord([]);
            }

            var trainerClassIds = await _context.TrainerClasses
                .AsNoTracking()
                .Where(item => item.TrainerId == trainer.TrainerId)
                .Select(item => item.ClassId)
                .ToListAsync();

            var assignedBatchRows = await _context.TrainerBatches
                .AsNoTracking()
                .Where(item => item.TrainerId == trainer.TrainerId)
                .Select(item => new
                {
                    item.Batch.BatchId,
                    item.Batch.ClassId,
                    item.Batch.CollegeId,
                    item.Batch.Name
                })
                .ToListAsync();

            var accessibleClassIds = trainerClassIds
                .Concat(assignedBatchRows.Select(item => item.ClassId))
                .Distinct()
                .ToList();

            if (accessibleClassIds.Count == 0)
            {
                return new AssessmentAssignmentCatalogRecord([]);
            }

            var classes = await _context.Classes
                .AsNoTracking()
                .Where(item => item.CollegeId == request.CollegeId && accessibleClassIds.Contains(item.ClassId))
                .OrderBy(item => item.Name)
                .ThenBy(item => item.AcademicYear)
                .Select(item => new
                {
                    item.ClassId,
                    item.CollegeId,
                    item.Name,
                    item.AcademicYear
                })
                .ToListAsync();

            var batchesByClass = assignedBatchRows
                .GroupBy(item => item.ClassId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderBy(item => item.Name)
                        .Select(item => new AssessmentAssignmentBatchRecord(
                            item.BatchId,
                            item.ClassId,
                            item.CollegeId,
                            item.Name))
                        .ToList());

            var classRecords = classes
                .Select(item => new AssessmentAssignmentClassRecord(
                    item.ClassId,
                    item.CollegeId,
                    item.Name,
                    item.AcademicYear,
                    batchesByClass.TryGetValue(item.ClassId, out var batches)
                        ? batches
                        : []))
                .ToList();

            return new AssessmentAssignmentCatalogRecord(classRecords);
        }, "retrieving trainer assigned classes and batches");
    }

    /// <inheritdoc />
    public async Task<PagedAssessmentSearchRecord> SearchAssessments(AssessmentSearchRequest request)
    {
        ValidateAssessmentSearchRequest(request);

        return await ExecuteDbOperationAsync(async () =>
        {
            var safePageNumber = request.PageNumber > 0 ? request.PageNumber : DefaultPageNumber;
            var safePageSize = request.PageSize is > 0 and <= MaximumPageSize ? request.PageSize : DefaultPageSize;
            var normalizedSearchTerm = request.SearchTerm?.Trim();
            var normalizedRequesterName = request.RequesterName.Trim();
            var statusFilter = NormalizeAssessmentStatusFilter(request.AssessmentStatus);

            var query = _context.Assessments
                .AsNoTracking()
                .Where(item => item.CollegeId == request.CollegeId)
                .Where(item => item.IsDeleted != true && item.AssessmentStatus != AssessmentStatus.Soft_Deleted);

            if (IsTrainer(request.RequesterRole))
            {
                var normalizedRequesterNameLower = normalizedRequesterName.ToLower();
                query = query.Where(item =>
                    item.CreatedBy != null &&
                    item.CreatedBy.Trim().ToLower() == normalizedRequesterNameLower);
            }
            else if (!IsCollegeAdmin(request.RequesterRole))
            {
                throw new UnauthorizedAccessException("Only CollegeAdmin and Trainer can access assessments.");
            }

            if (statusFilter.HasValue)
            {
                query = query.Where(item => item.AssessmentStatus == statusFilter.Value);
            }

            if (request.DifficultyLevel.HasValue)
            {
                query = query.Where(item => item.DifficultyLevel == request.DifficultyLevel.Value);
            }

            var assessments = await query
                .OrderByDescending(item => item.StartDateTime ?? item.CreatedAt)
                .ThenByDescending(item => item.CreatedAt)
                .ToListAsync();

            await PopulateAssessmentLabelsAsync(assessments);

            if (!string.IsNullOrWhiteSpace(normalizedSearchTerm))
            {
                assessments = assessments
                    .Where(item =>
                        item.AssessmentName.Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        item.SubjectNames.Any(name => name.Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        item.TopicNames.Any(name => name.Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        (item.SubjectName?.Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (item.TopicName?.Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();
            }

            var totalCount = assessments.Count;
            var activeCount = assessments.Count(item =>
                item.AssessmentStatus == AssessmentStatus.Live ||
                item.AssessmentStatus == AssessmentStatus.Scheduled);
            var completedCount = assessments.Count(item => item.AssessmentStatus == AssessmentStatus.Completed);

            assessments = assessments
                .Skip((safePageNumber - 1) * safePageSize)
                .Take(safePageSize)
                .ToList();

            return new PagedAssessmentSearchRecord(
                assessments.Select(item => item.ToSearchItemRecord()).ToList(),
                totalCount,
                activeCount,
                completedCount,
                safePageNumber,
                safePageSize);
        }, "searching assessments");
    }

    /// <inheritdoc />
    public async Task<PagedAssessmentQuestionListRecord> GetAssessmentQuestionList(
        Guid assessmentId,
        int pageNumber,
        int pageSize)
    {
        return await ExecuteDbOperationAsync(async () =>
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
        }, "retrieving assessment question list");
    }

    /// <inheritdoc />
    public async Task<List<StudentAssessmentListItemRecord>> GetStudentAssessments(
        Guid studentUserId,
        IReadOnlyCollection<string> assessmentStatuses)
    {
        if (studentUserId == Guid.Empty)
        {
            throw new ArgumentException("Student user id is required.");
        }

        return await ExecuteDbOperationAsync(async () =>
        {
            var normalizedStatuses = NormalizeStudentAssessmentStatuses(assessmentStatuses);

            var student = await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.UserId == studentUserId);

            if (student is null)
            {
                throw new KeyNotFoundException($"Student profile was not found for user '{studentUserId}'.");
            }

            return normalizedStatuses.Contains(nameof(AssessmentStatus.Completed))
                ? await GetCompletedStudentAssessmentsAsync(student, student.BatchId)
                : await GetActiveStudentAssessmentsAsync(student, student.BatchId, normalizedStatuses);
        }, "retrieving student assessments");
    }

    /// <inheritdoc />
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

        return await ExecuteDbOperationAsync(async () =>
        {
            var student = await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.UserId == studentUserId);

            if (student is null)
            {
                throw new KeyNotFoundException($"Student profile was not found for user '{studentUserId}'.");
            }

            var assessment = await BuildStudentAssessmentQuery(student.CollegeId, student.BatchId)
                .Include(item => item.AssessmentQuestions)
                .FirstOrDefaultAsync(item =>
                    item.AssessmentId == assessmentId &&
                    (item.AssessmentStatus == AssessmentStatus.Scheduled ||
                     item.AssessmentStatus == AssessmentStatus.Live ||
                     item.AssessmentStatus == AssessmentStatus.Completed));

            if (assessment is null)
            {
                throw new KeyNotFoundException($"Assessment '{assessmentId}' was not found for the current student.");
            }

            return assessment.ToStudentAssessmentDetailRecord(assessment.AssessmentQuestions.Count);
        }, "retrieving student assessment detail");
    }

    /// <inheritdoc />
    public async Task<StudentAssessmentStartRecord> StartStudentAssessment(Guid assessmentId, Guid studentUserId)
    {
        ValidateStudentAttemptRequest(assessmentId, studentUserId);

        return await ExecuteDbOperationAsync(async () =>
        {
            var student = await GetStudentByUserIdAsync(studentUserId);
            var assessment = await GetStudentAssessmentForAttemptAsync(assessmentId, student);

            var latestAttempt = await GetLatestAttemptAsync(student.StudentId, assessmentId);
            if (latestAttempt is not null)
            {
                if (latestAttempt.AttemptStatus is AttemptStatus.Submitted or AttemptStatus.Auto_Submitted)
                {
                    throw new InvalidOperationException("This assessment has already been submitted by the current student.");
                }

                if (await TryRestartExpiredUnengagedAttemptAsync(latestAttempt, assessment))
                {
                    return latestAttempt.ToStudentAssessmentStartRecord();
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
        }, "starting the student assessment attempt");
    }

    /// <inheritdoc />
    public async Task<StudentAttemptRecoveryRecord> GetStudentAttemptRecovery(Guid attemptId, Guid studentUserId)
    {
        if (attemptId == Guid.Empty)
        {
            throw new ArgumentException("Attempt id is required.");
        }

        ValidateStudentAttemptRequest(Guid.Empty, studentUserId, validateAssessmentId: false);

        return await ExecuteDbOperationAsync(async () =>
        {
            var student = await GetStudentByUserIdAsync(studentUserId);
            var attempt = await GetAttemptForStudentAsync(attemptId, student.StudentId);
            var assessment = await GetAssessmentForAttemptRecoveryAsync(attempt.AssessmentId, student);

            await TryRestartExpiredUnengagedAttemptAsync(attempt, assessment);
            await EnsureAttemptClosedIfExpiredAsync(attempt, assessment);

            if (attempt.AttemptStatus is AttemptStatus.Submitted or AttemptStatus.Auto_Submitted)
            {
                attempt = await GetAttemptForStudentAsync(attemptId, student.StudentId);
            }

            return await BuildStudentAttemptRecoveryAsync(attempt, assessment);
        }, "recovering the student assessment attempt");
    }

    public async Task<ProctorSessionStateRecord> GetAttemptProctorSession(Guid attemptId, Guid collegeId, string requesterRole, string requesterName)
    {
        if (attemptId == Guid.Empty)
        {
            throw new ArgumentException("AttemptId is required.");
        }

        if (collegeId == Guid.Empty)
        {
            throw new ArgumentException("CollegeId is required.");
        }

        if (string.IsNullOrWhiteSpace(requesterRole))
        {
            throw new ArgumentException("RequesterRole is required.");
        }

        if (string.IsNullOrWhiteSpace(requesterName))
        {
            throw new ArgumentException("RequesterName is required.");
        }

        return await ExecuteDbOperationAsync(async () =>
        {
            var attempt = await _context.Attempts
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.AttemptId == attemptId);

            if (attempt is null)
            {
                throw new KeyNotFoundException($"Attempt '{attemptId}' was not found.");
            }

            var assessment = await _context.Assessments
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.AssessmentId == attempt.AssessmentId);

            if (assessment is null)
            {
                throw new KeyNotFoundException($"Assessment '{attempt.AssessmentId}' was not found.");
            }

            EnsureAssessmentReadAuthorized(assessment, collegeId, requesterRole, requesterName);

            var session = await _proctorServiceClient.GetSessionByAttemptAsync(attemptId);
            return session ?? throw new KeyNotFoundException($"Proctoring session for attempt '{attemptId}' was not found.");
        }, "retrieving the attempt proctoring session");
    }

    /// <inheritdoc />
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

        return await ExecuteDbOperationAsync(async () =>
        {
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
            var savedAnswer = await strategy.SaveAsync(_context, attempt, assessment, question, request, answeredAt);

            attempt.LastActivityAt = answeredAt;
            await RefreshAttemptProgressAsync(attempt);

            await SaveChangesWithWrapAsync("Unable to save the assessment answer.");

            return savedAnswer.ToStudentAttemptAnswerRecord();
        }, "saving the student assessment answer");
    }

    /// <inheritdoc />
    public async Task<StudentAttemptSubmitRecord> SubmitStudentAttempt(Guid attemptId, Guid studentUserId)
    {
        if (attemptId == Guid.Empty)
        {
            throw new ArgumentException("Attempt id is required.");
        }

        ValidateStudentAttemptRequest(Guid.Empty, studentUserId, validateAssessmentId: false);

        return await ExecuteDbOperationAsync(async () =>
        {
            var student = await GetStudentByUserIdAsync(studentUserId);
            var attempt = await GetAttemptForStudentAsync(attemptId, student.StudentId);
            var assessment = await GetAssessmentForAttemptRecoveryAsync(attempt.AssessmentId, student);

            if (attempt.AttemptStatus is AttemptStatus.Submitted or AttemptStatus.Auto_Submitted)
            {
                return attempt.ToStudentAttemptSubmitRecord();
            }

            var submittedAt = DateTime.UtcNow;
            var isExpired = IsAttemptExpired(attempt, assessment, out var expiresAt);
            var finalStatus = isExpired ? AttemptStatus.Auto_Submitted : AttemptStatus.Submitted;
            var effectiveSubmittedAt = isExpired ? expiresAt : submittedAt;
            var effectiveExpiresAt = isExpired
                ? expiresAt
                : ResolveAttemptExpiresAt(attempt, assessment, submittedAt);

            var finalizedAttempt = await FinalizeAttemptAsync(attempt, effectiveSubmittedAt, effectiveExpiresAt, finalStatus);
            return finalizedAttempt.ToStudentAttemptSubmitRecord();
        }, "submitting the student assessment attempt");
    }

    /// <inheritdoc />
    public async Task<StudentStreakRecord> GetStudentStreak(Guid studentUserId)
    {
        return await ExecuteDbOperationAsync(async () =>
        {
            var student = await GetStudentByUserIdAsync(studentUserId);
            return new StudentStreakRecord(
                student.CurrentStreak ?? 0,
                student.LongestStreak ?? 0,
                student.TotalAssessmentsTaken ?? 0);
        }, "retrieving the student streak summary");
    }

    private static Guid[] NormalizeAssignedBatchIds(IEnumerable<Guid>? assignedBatchIds)
    {
        return (assignedBatchIds ?? [])
            .Where(batchId => batchId != Guid.Empty)
            .Distinct()
            .ToArray();
    }

    private static Guid[] NormalizeClassificationIds(IEnumerable<Guid>? ids)
    {
        return (ids ?? [])
            .Where(id => id != Guid.Empty)
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
        List<Guid> questionIds)
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
            .Where(question => !question.IsActive)
            .Select(question => question.QuestionId)
            .ToList();

        if (unavailableQuestionIds.Count > 0)
        {
            throw new InvalidOperationException(
                $"Question(s) are not available in the question bank: {string.Join(", ", unavailableQuestionIds)}.");
        }
    }

    private async Task PopulateAssessmentClassificationAsync(Assessment assessment, IReadOnlyCollection<Question> questions)
    {
        var explicitSubjectIds = NormalizeClassificationIds(assessment.SubjectIds);
        var explicitTopicIds = NormalizeClassificationIds(assessment.TopicIds);

        var questionSubjectIds = questions
            .Where(question => question.SubjectId.HasValue)
            .Select(question => question.SubjectId!.Value);

        var questionTopicIds = questions
            .Where(question => question.TopicId.HasValue)
            .Select(question => question.TopicId!.Value);

        var resolvedTopicIds = NormalizeClassificationIds(explicitTopicIds.Concat(questionTopicIds));
        var topics = resolvedTopicIds.Length == 0
            ? []
            : await _context.Topics
                .AsNoTracking()
                .Where(topic => resolvedTopicIds.Contains(topic.TopicId))
                .Select(topic => new { topic.TopicId, topic.TopicName, topic.SubjectId })
                .ToListAsync();

        var missingTopicIds = explicitTopicIds.Except(topics.Select(topic => topic.TopicId)).ToArray();
        if (missingTopicIds.Length > 0)
        {
            throw new InvalidOperationException(
                $"Topic id(s) were not found for this assessment: {string.Join(", ", missingTopicIds)}.");
        }

        var resolvedSubjectIds = NormalizeClassificationIds(
            explicitSubjectIds
                .Concat(questionSubjectIds)
                .Concat(topics.Select(topic => topic.SubjectId)));

        var subjects = resolvedSubjectIds.Length == 0
            ? []
            : await _context.Subjects
                .AsNoTracking()
                .Where(subject => resolvedSubjectIds.Contains(subject.SubjectId))
                .Select(subject => new { subject.SubjectId, subject.SubjectName })
                .ToListAsync();

        var missingSubjectIds = explicitSubjectIds.Except(subjects.Select(subject => subject.SubjectId)).ToArray();
        if (missingSubjectIds.Length > 0)
        {
            throw new InvalidOperationException(
                $"Subject id(s) were not found for this assessment: {string.Join(", ", missingSubjectIds)}.");
        }

        var subjectNameMap = subjects.ToDictionary(subject => subject.SubjectId, subject => subject.SubjectName);
        var topicNameMap = topics.ToDictionary(topic => topic.TopicId, topic => topic.TopicName);

        assessment.SubjectIds = resolvedSubjectIds;
        assessment.TopicIds = resolvedTopicIds;
        ApplyAssessmentLabels(assessment, subjectNameMap, topicNameMap);
    }

    private async Task PopulateAssessmentLabelsAsync(IReadOnlyCollection<Assessment> assessments)
    {
        if (assessments.Count == 0)
        {
            return;
        }

        var subjectIds = assessments
            .SelectMany(assessment => NormalizeClassificationIds(assessment.SubjectIds))
            .Distinct()
            .ToArray();

        var topicIds = assessments
            .SelectMany(assessment => NormalizeClassificationIds(assessment.TopicIds))
            .Distinct()
            .ToArray();

        var subjectNameMap = subjectIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await _context.Subjects
                .AsNoTracking()
                .Where(subject => subjectIds.Contains(subject.SubjectId))
                .ToDictionaryAsync(subject => subject.SubjectId, subject => subject.SubjectName);

        var topicNameMap = topicIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await _context.Topics
                .AsNoTracking()
                .Where(topic => topicIds.Contains(topic.TopicId))
                .ToDictionaryAsync(topic => topic.TopicId, topic => topic.TopicName);

        foreach (var assessment in assessments)
        {
            assessment.SubjectIds = NormalizeClassificationIds(assessment.SubjectIds);
            assessment.TopicIds = NormalizeClassificationIds(assessment.TopicIds);
            ApplyAssessmentLabels(assessment, subjectNameMap, topicNameMap);
        }
    }

    private static void ApplyAssessmentLabels(
        Assessment assessment,
        IReadOnlyDictionary<Guid, string> subjectNameMap,
        IReadOnlyDictionary<Guid, string> topicNameMap)
    {
        assessment.SubjectNames = assessment.SubjectIds
            .Where(subjectNameMap.ContainsKey)
            .Select(subjectId => subjectNameMap[subjectId])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        assessment.TopicNames = assessment.TopicIds
            .Where(topicNameMap.ContainsKey)
            .Select(topicId => topicNameMap[topicId])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        assessment.SubjectName = BuildAssessmentLabel(assessment.SubjectNames, "Subject", "Subjects");
        assessment.TopicName = BuildAssessmentLabel(assessment.TopicNames, "Topic", "Topics");
    }

    private static string? BuildAssessmentLabel(IReadOnlyCollection<string> names, string singularLabel, string pluralLabel)
    {
        if (names.Count == 0)
        {
            return null;
        }

        return names.Count == 1 ? names.First() : $"Multiple {pluralLabel}";
    }

    private IQueryable<Batch> BuildAccessibleBatchQuery(AssessmentAccessibleBatchesRequest request)
    {
        var normalizedRole = request.RequesterRole.Trim();

        var query = _context.Batches
            .AsNoTracking()
            .Where(batch => batch.CollegeId == request.CollegeId);

        if (!string.Equals(normalizedRole, "Trainer", StringComparison.OrdinalIgnoreCase))
        {
            return query;
        }

        // Build a subquery for batch IDs assigned to this trainer, avoiding the
        // navigation-property filter (batch.TrainerBatches.Any(...)) which can cause
        // EF Core translation errors when combined with .Include() on the same property.
        var assignedBatchIds = _context.TrainerBatches
            .AsNoTracking()
            .Where(link =>
                link.Trainer.UserId == request.RequesterUserId &&
                link.Trainer.CollegeId == request.CollegeId)
            .Select(link => link.BatchId);

        return query.Where(batch => assignedBatchIds.Contains(batch.BatchId));
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

    private static void ValidateTrainerAssignmentRequest(AssessmentAccessibleBatchesRequest request)
    {
        ValidateAccessibleBatchesRequest(request);

        if (!string.Equals(request.RequesterRole.Trim(), "Trainer", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Trainer role is required for trainer assignment requests.");
        }
    }

    private static void ValidateAssessmentSearchRequest(AssessmentSearchRequest request)
    {
        if (request is null)
        {
            throw new ArgumentException("Assessment search request is required.");
        }

        if (request.CollegeId == Guid.Empty)
        {
            throw new ArgumentException("CollegeId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.RequesterRole))
        {
            throw new ArgumentException("RequesterRole is required.");
        }

        if (string.IsNullOrWhiteSpace(request.RequesterName))
        {
            throw new ArgumentException("RequesterName is required.");
        }

        if (request.DifficultyLevel.HasValue && request.DifficultyLevel.Value is < 1 or > 3)
        {
            throw new ArgumentException("DifficultyLevel must be between 1 and 3.");
        }
    }

    private static void PrepareAssessmentForCreation(
        Assessment assessment,
        IEnumerable<Question> questions,
        AssessmentStatus assessmentStatus)
    {
        var now = DateTime.UtcNow;
        assessment.AssessmentId = assessment.AssessmentId == Guid.Empty ? Guid.NewGuid() : assessment.AssessmentId;
        assessment.AssessmentType = ResolveAssessmentType(questions);
        assessment.DifficultyLevel = CalculateDifficultyLevel(questions);
        assessment.AssessmentStatus = assessmentStatus;
        assessment.CreatedAt = now;
        assessment.ModifiedAt = now;
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

    private static void ApplyAssessmentUpdates(
        Assessment target,
        Assessment source,
        IEnumerable<Question> questions)
    {
        target.SubjectIds = source.SubjectIds;
        target.SubjectNames = source.SubjectNames;
        target.SubjectName = source.SubjectName;
        target.TopicIds = source.TopicIds;
        target.TopicNames = source.TopicNames;
        target.TopicName = source.TopicName;
        target.AssessmentName = source.AssessmentName;
        target.AssessmentType = ResolveAssessmentType(questions);
        target.AssessmentStatus = source.AssessmentStatus;
        target.DurationMinutes = source.DurationMinutes;
        target.TotalMarks = source.TotalMarks;
        target.DifficultyLevel = CalculateDifficultyLevel(questions);
        target.StartDateTime = source.StartDateTime;
        target.EndDateTime = source.EndDateTime;
        target.Instructions = source.Instructions;
        target.AssignedBatchIds = source.AssignedBatchIds;
        target.AllowLateEntry = source.AllowLateEntry;
        target.ShowResultsImmediately = source.ShowResultsImmediately;
        target.PassingPercentage = source.PassingPercentage;
        target.AllowQuestionReview = source.AllowQuestionReview;
        target.NegativeMarking = source.NegativeMarking;
        target.IsTotalMarksAutoCalculated = source.IsTotalMarksAutoCalculated;
        target.ModifiedAt = source.ModifiedAt;
    }

    private static void ApplyPartialDraftUpdates(Assessment target, Assessment source)
    {
        target.SubjectIds = source.SubjectIds;
        target.SubjectNames = source.SubjectNames;
        target.SubjectName = source.SubjectName;
        target.TopicIds = source.TopicIds;
        target.TopicNames = source.TopicNames;
        target.TopicName = source.TopicName;
        target.AssessmentName = source.AssessmentName;
        target.AssessmentType = AssessmentType.Objective;
        target.AssessmentStatus = AssessmentStatus.Draft;
        target.DurationMinutes = source.DurationMinutes;
        target.TotalMarks = source.TotalMarks;
        target.DifficultyLevel = 1;
        target.StartDateTime = source.StartDateTime;
        target.EndDateTime = source.EndDateTime;
        target.Instructions = source.Instructions;
        target.AssignedBatchIds = source.AssignedBatchIds;
        target.AllowLateEntry = source.AllowLateEntry;
        target.ShowResultsImmediately = source.ShowResultsImmediately;
        target.PassingPercentage = source.PassingPercentage;
        target.AllowQuestionReview = source.AllowQuestionReview;
        target.NegativeMarking = source.NegativeMarking;
        target.IsTotalMarksAutoCalculated = source.IsTotalMarksAutoCalculated;
        target.ModifiedAt = source.ModifiedAt;
    }

    private async Task SyncAssessmentQuestionsAsync(
        Assessment assessment,
        IReadOnlyCollection<Question> questions,
        IReadOnlyList<Guid> orderedQuestionIds)
    {
        var existingAssessmentQuestions = assessment.AssessmentQuestions.ToList();

        if (existingAssessmentQuestions.Count > 0)
        {
            foreach (var existingAssessmentQuestion in existingAssessmentQuestions)
            {
                _context.Entry(existingAssessmentQuestion).State = EntityState.Detached;
            }

            await _context.AssessmentQuestions
                .Where(item => item.AssessmentId == assessment.AssessmentId)
                .ExecuteDeleteAsync();
        }

        assessment.AssessmentQuestions.Clear();

        var rebuiltAssessmentQuestions = BuildAssessmentQuestions(assessment.AssessmentId, questions, orderedQuestionIds);
        _context.AssessmentQuestions.AddRange(rebuiltAssessmentQuestions);

        foreach (var assessmentQuestion in rebuiltAssessmentQuestions)
        {
            assessment.AssessmentQuestions.Add(assessmentQuestion);
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

    private async Task<Assessment> GetAssessmentForUpdateAsync(Guid assessmentId)
    {
        var assessment = await _context.Assessments
            .Include(item => item.AssessmentQuestions)
            .FirstOrDefaultAsync(item => item.AssessmentId == assessmentId);

        if (assessment is null)
        {
            throw new KeyNotFoundException($"Assessment '{assessmentId}' was not found.");
        }

        return assessment;
    }

    private static void EnsureUpdateAuthorized(Assessment assessment, UpdateAssessmentRequest request)
    {
        if (IsTrainer(request.RequesterRole))
        {
            if (assessment.CollegeId != request.CollegeId)
            {
                throw new UnauthorizedAccessException("Trainer can update assessments only for its own college.");
            }

            if (!string.Equals(assessment.CreatedBy?.Trim(), request.UpdatedBy?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Trainer can update only the assessments created by the same trainer.");
            }

            return;
        }

        if (IsCollegeAdmin(request.RequesterRole))
        {
            if (assessment.CollegeId != request.CollegeId)
            {
                throw new UnauthorizedAccessException("College admin can update assessments only for its own college.");
            }

            return;
        }

        throw new UnauthorizedAccessException("Only CollegeAdmin and Trainer can update assessments.");
    }

    private static void EnsureAssessmentReadAuthorized(
        Assessment assessment,
        Guid collegeId,
        string requesterRole,
        string requesterName)
    {
        if (IsTrainer(requesterRole))
        {
            if (assessment.CollegeId != collegeId)
            {
                throw new UnauthorizedAccessException("Trainer can access assessments only for its own college.");
            }

            if (!string.Equals(assessment.CreatedBy?.Trim(), requesterName?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Trainer can access only the assessments created by the same trainer.");
            }

            return;
        }

        if (IsCollegeAdmin(requesterRole))
        {
            if (assessment.CollegeId != collegeId)
            {
                throw new UnauthorizedAccessException("College admin can access assessments only for its own college.");
            }

            return;
        }

        throw new UnauthorizedAccessException("Only CollegeAdmin and Trainer can access assessments.");
    }

    private static void EnsureAssessmentCanBeDeleted(Assessment assessment)
    {
        if (assessment.AssessmentStatus == AssessmentStatus.Soft_Deleted || assessment.IsDeleted == true)
        {
            throw new InvalidOperationException("This assessment has already been soft deleted.");
        }

        if (assessment.AssessmentStatus is not AssessmentStatus.Draft and not AssessmentStatus.Scheduled)
        {
            throw new InvalidOperationException("Only draft or scheduled assessments can be deleted.");
        }

        if (assessment.AssessmentStatus != AssessmentStatus.Scheduled)
        {
            return;
        }

        if (!assessment.StartDateTime.HasValue)
        {
            throw new InvalidOperationException(
                "This scheduled assessment cannot be deleted because its start time is unavailable.");
        }

        var deleteCutoffTime = assessment.StartDateTime.Value.AddMinutes(-15);
        if (DateTime.UtcNow > deleteCutoffTime)
        {
            throw new InvalidOperationException(
                "Scheduled assessments can only be deleted until 15 minutes before the start time.");
        }
    }

    private static void EnsureAssessmentCanBeUpdated(Assessment assessment)
    {
        if (assessment.AssessmentStatus is not AssessmentStatus.Draft and not AssessmentStatus.Scheduled)
        {
            throw new InvalidOperationException("Only draft or scheduled assessments can be edited.");
        }

        if (assessment.AssessmentStatus != AssessmentStatus.Scheduled)
        {
            return;
        }

        if (!assessment.StartDateTime.HasValue)
        {
            throw new InvalidOperationException(
                "This scheduled assessment cannot be edited because its start time is unavailable.");
        }

        var editCutoffTime = assessment.StartDateTime.Value.AddMinutes(-15);
        if (DateTime.UtcNow > editCutoffTime)
        {
            throw new InvalidOperationException(
                "Scheduled assessments can only be edited until 15 minutes before the start time.");
        }
    }

    private static void EnsureDeleteAuthorized(Assessment assessment, DeleteAssessmentRequest request)
    {
        if (IsTrainer(request.RequesterRole))
        {
            if (!request.CollegeId.HasValue || request.CollegeId.Value == Guid.Empty)
            {
                throw new ArgumentException("CollegeId is required for trainer delete operations.");
            }

            if (assessment.CollegeId != request.CollegeId.Value)
            {
                throw new UnauthorizedAccessException("Trainer can delete assessments only for its own college.");
            }

            if (!string.Equals(assessment.CreatedBy?.Trim(), request.DeletedBy?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Trainer can delete only the assessments created by the same trainer.");
            }

            return;
        }

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
            throw new UnauthorizedAccessException("Only SuperAdmin, CollegeAdmin, and Trainer can delete assessments.");
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
            var linkedQuestionIds = assessment.AssessmentQuestions
                .OrderBy(item => item.DisplayOrder)
                .Select(item => item.QuestionId)
                .ToList();

            var linkedQuestions = await _context.Questions
                .Where(question => linkedQuestionIds.Contains(question.QuestionId))
                .ToListAsync();

            EnsureQuestionsExist(linkedQuestionIds, linkedQuestions);

            var publishedQuestionLookup = linkedQuestions.ToDictionary(question => question.QuestionId);
            return linkedQuestionIds.Select(questionId => publishedQuestionLookup[questionId]).ToList();
        }

        var persistedQuestionIds = await _context.AssessmentQuestions
            .AsNoTracking()
            .Where(item => item.AssessmentId == assessment.AssessmentId)
            .OrderBy(item => item.DisplayOrder)
            .Select(item => item.QuestionId)
            .ToListAsync();

        if (persistedQuestionIds.Count == 0)
        {
            return [];
        }

        var persistedQuestions = await _context.Questions
            .Where(question => persistedQuestionIds.Contains(question.QuestionId))
            .ToListAsync();

        EnsureQuestionsExist(persistedQuestionIds, persistedQuestions);

        var persistedQuestionLookup = persistedQuestions.ToDictionary(question => question.QuestionId);
        return persistedQuestionIds
            .Select(questionId => persistedQuestionLookup[questionId])
            .ToList();
    }

    private async Task<List<StudentAssessmentListItemRecord>> GetCompletedStudentAssessmentsAsync(Student student, Guid? batchId)
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

        await PopulateAssessmentLabelsAsync(completedAssessments);

        return completedAssessments
            .Select(assessment => ToStudentAssessmentListItem(assessment, nameof(AssessmentStatus.Completed)))
            .ToList();
    }

    private async Task<List<StudentAssessmentListItemRecord>> GetActiveStudentAssessmentsAsync(
        Student student,
        Guid? batchId,
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

        await PopulateAssessmentLabelsAsync(assessments);

        return assessments
            .Select(assessment => ToStudentAssessmentListItem(assessment, assessment.AssessmentStatus.ToString()))
            .ToList();
    }

    private IQueryable<Assessment> BuildStudentAssessmentQuery(Guid collegeId, Guid? batchId)
    {
        return _context.Assessments
            .AsNoTracking()
            .Where(assessment =>
                assessment.CollegeId == collegeId &&
                assessment.AssessmentStatus != AssessmentStatus.Soft_Deleted &&
                (assessment.AssignedBatchIds.Length == 0 ||
                 (batchId.HasValue && assessment.AssignedBatchIds.Contains(batchId.Value))));
    }

    private static StudentAssessmentListItemRecord ToStudentAssessmentListItem(Assessment assessment, string assessmentStatus)
    {
        return new StudentAssessmentListItemRecord(
            assessment.AssessmentId,
            assessment.AssessmentName,
            assessment.SubjectName,
            assessment.TopicName,
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
        var assessment = await BuildStudentAssessmentQuery(student.CollegeId, student.BatchId)
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
        var assessment = await BuildStudentAssessmentQuery(student.CollegeId, student.BatchId)
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
        var expiresAt = CalculateAttemptExpiresAt(startedAt, assessment);

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

    private async Task<bool> TryRestartExpiredUnengagedAttemptAsync(Attempt attempt, Assessment assessment)
    {
        if (attempt.AttemptStatus is not AttemptStatus.In_Progress)
        {
            return false;
        }

        if (!IsAttemptExpired(attempt, assessment, out _))
        {
            return false;
        }

        if (await HasMeaningfulAttemptEngagementAsync(attempt))
        {
            return false;
        }

        var restartedAt = DateTime.UtcNow;
        attempt.StartedAt = restartedAt;
        attempt.LastActivityAt = restartedAt;
        attempt.ExpiresAt = CalculateAttemptExpiresAt(restartedAt, assessment);
        attempt.SubmittedAt = null;
        attempt.AttemptStatus = AttemptStatus.In_Progress;
        attempt.AttemptedQuestions = 0;
        attempt.CorrectAnswers = 0;
        attempt.WrongAnswers = 0;
        attempt.UnansweredQuestions = attempt.TotalQuestions;
        attempt.TotalScore = 0;
        attempt.Percentage = 0;
        attempt.TimeTakenSeconds = 0;
        attempt.IsPassed = false;

        await SaveChangesWithWrapAsync("Unable to restart the expired assessment attempt.");
        return true;
    }

    private async Task<bool> HasMeaningfulAttemptEngagementAsync(Attempt attempt)
    {
        if (attempt.AttemptedQuestions > 0)
        {
            return true;
        }

        var hasSavedAnswers = await _context.AttemptAnswers
            .AsNoTracking()
            .AnyAsync(item => item.AttemptId == attempt.AttemptId);

        if (hasSavedAnswers)
        {
            return true;
        }

        var latestSession = await _context.ProctoringSessions
            .AsNoTracking()
            .Where(item => item.AttemptId == attempt.AttemptId)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync();

        if (latestSession is null)
        {
            return false;
        }

        return latestSession.LastHeartbeatAt.HasValue
            || latestSession.LastKnownQuestionId.HasValue
            || latestSession.EndedAt.HasValue
            || latestSession.ModifiedAt.HasValue;
    }

    private static bool IsAttemptExpired(Attempt attempt, Assessment assessment, out DateTime expiresAt)
    {
        expiresAt = ResolveAttemptExpiresAt(attempt, assessment, DateTime.UtcNow);

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

        var expiresAt = ResolveAttemptExpiresAt(attempt, assessment, DateTime.UtcNow);

        attempt.ExpiresAt = expiresAt;

        var remainingSeconds = attempt.AttemptStatus == AttemptStatus.In_Progress
            ? Math.Max(0, (int)Math.Ceiling((expiresAt - DateTime.UtcNow).TotalSeconds))
            : 0;

        return attempt.ToStudentAttemptRecoveryRecord(assessment, remainingSeconds, questionRecords);
    }

    private static DateTime CalculateAttemptExpiresAt(DateTime startedAt, Assessment assessment)
    {
        var durationExpiry = startedAt.AddMinutes(assessment.DurationMinutes);

        if (assessment.EndDateTime.HasValue)
        {
            return assessment.EndDateTime.Value < durationExpiry
                ? assessment.EndDateTime.Value
                : durationExpiry;
        }

        return durationExpiry;
    }

    private static DateTime ResolveAttemptExpiresAt(Attempt attempt, Assessment assessment, DateTime fallbackStartedAt)
    {
        if (attempt.StartedAt.HasValue)
        {
            return CalculateAttemptExpiresAt(attempt.StartedAt.Value, assessment);
        }

        if (attempt.ExpiresAt.HasValue)
        {
            return attempt.ExpiresAt.Value;
        }

        return CalculateAttemptExpiresAt(fallbackStartedAt, assessment);
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
            _logger.LogError(ex, "{ErrorMessage} SaveChanges failed in AssessmentManager.", errorMessage);
            throw new InvalidOperationException(errorMessage, ex);
        }
    }

    private async Task<T> ExecuteDbOperationAsync<T>(Func<Task<T>> operation, string operationName)
    {
        try
        {
            return await operation();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update error while {OperationName} in AssessmentManager.", operationName);
            throw;
        }
        catch (PostgresException ex)
        {
            _logger.LogError(ex, "PostgreSQL error while {OperationName} in AssessmentManager.", operationName);
            throw;
        }
    }

    private async Task ExecuteDbOperationAsync(Func<Task> operation, string operationName)
    {
        try
        {
            await operation();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update error while {OperationName} in AssessmentManager.", operationName);
            throw;
        }
        catch (PostgresException ex)
        {
            _logger.LogError(ex, "PostgreSQL error while {OperationName} in AssessmentManager.", operationName);
            throw;
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
                    return currentAttempt;
                }

                throw new InvalidOperationException("This assessment attempt could not be submitted.");
            }

            await _context.Entry(attempt).ReloadAsync();

            var resultProcessingContext = await GetResultProcessingContextAsync(attempt.AssessmentId);
            await ProcessAttemptResultAsync(attempt.AttemptId, attempt.StudentId, resultProcessingContext);
            await UpdateStudentStreakAsync(attempt.StudentId, submittedAt);
            return attempt;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Unable to submit the assessment attempt.", ex);
        }
    }

    private async Task UpdateStudentStreakAsync(Guid studentId, DateTime submittedAtUtc)
    {
        var student = await _context.Students.FirstOrDefaultAsync(item => item.StudentId == studentId);
        if (student is null)
        {
            return;
        }

        var today = submittedAtUtc.Date;
        var lastAssessmentDate = student.LastAssessmentDate?.Date;

        var updatedStreak = lastAssessmentDate switch
        {
            null => 1,
            var lastDate when lastDate == today => student.CurrentStreak ?? 1,
            var lastDate when lastDate == today.AddDays(-1) => (student.CurrentStreak ?? 0) + 1,
            _ => 1
        };

        student.CurrentStreak = updatedStreak;
        student.LongestStreak = Math.Max(student.LongestStreak ?? 0, updatedStreak);
        student.LastAssessmentDate = today;
        student.TotalAssessmentsTaken = (student.TotalAssessmentsTaken ?? 0) + 1;

        await _context.SaveChangesAsync();
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

    private async Task<ResultProcessingContext> GetResultProcessingContextAsync(Guid assessmentId)
    {
        return await _context.Assessments
            .AsNoTracking()
            .Where(item => item.AssessmentId == assessmentId)
            .Select(item => new ResultProcessingContext(
                item.PassingPercentage,
                item.ShowResultsImmediately))
            .FirstAsync();
    }

    private async Task ProcessAttemptResultAsync(
        Guid attemptId,
        Guid studentId,
        ResultProcessingContext resultProcessingContext)
    {
        AttemptEvaluationResultClientModel? evaluationResult;

        try
        {
            evaluationResult = await _reportsServiceClient.EvaluateAttemptAsync(
                attemptId,
                resultProcessingContext.PassingPercentage);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Attempt {AttemptId} was submitted, but result evaluation did not complete.",
                attemptId);
            throw new InvalidOperationException(
                $"Result evaluation did not complete for attempt '{attemptId}'.",
                ex);
        }

        if (!resultProcessingContext.ShowResultsImmediately)
        {
            return;
        }

        if (evaluationResult is null)
        {
            _logger.LogInformation(
                "Attempt {AttemptId} evaluation completed without an immediate result payload for student {StudentId}.",
                attemptId,
                studentId);
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

        ValidatePassingPercentage(assessment.PassingPercentage);

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

    private static void ValidatePartialDraft(Assessment assessment)
    {
        if (assessment.CollegeId == Guid.Empty)
        {
            throw new ArgumentException("CollegeId is required.");
        }

        if (string.IsNullOrWhiteSpace(assessment.CreatedBy))
        {
            throw new ArgumentException("CreatedBy is required.");
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

    private static void ValidateUpdateAssessmentRequest(UpdateAssessmentRequest request)
    {
        if (request.AssessmentId == Guid.Empty)
        {
            throw new ArgumentException("AssessmentId is required.");
        }

        if (request.CollegeId == Guid.Empty)
        {
            throw new ArgumentException("CollegeId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.UpdatedBy))
        {
            throw new ArgumentException("UpdatedBy is required.");
        }

        if (string.IsNullOrWhiteSpace(request.RequesterRole))
        {
            throw new ArgumentException("RequesterRole is required.");
        }
    }

    private static void ApplyAutoCalculatedTotalMarksIfEnabled(Assessment assessment, IReadOnlyCollection<Question> questions)
    {
        if (assessment.IsTotalMarksAutoCalculated != true)
        {
            return;
        }

        assessment.TotalMarks = CalculatePersistedTotalMarks(questions);
    }

    private static int CalculatePersistedTotalMarks(IEnumerable<Question> questions)
    {
        var selectedMarks = questions.Sum(question => question.Marks);

        if (selectedMarks < 0)
        {
            throw new InvalidDataException("Selected question marks cannot produce a negative total.");
        }

        if (decimal.Truncate(selectedMarks) != selectedMarks)
        {
            throw new InvalidDataException(
                $"Selected question marks sum to {selectedMarks}, but assessment total marks currently support only whole numbers.");
        }

        if (selectedMarks > int.MaxValue)
        {
            throw new InvalidDataException("Selected question marks exceed the supported total marks range.");
        }

        return decimal.ToInt32(selectedMarks);
    }

    private void ValidateQuestionBudget(Assessment assessment, List<Question> questions)
    {
        var selectedQuestionCount = questions.Count;
        var selectedMarks = questions.Sum(question => question.Marks);

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

    private static void ValidatePassingPercentage(int passingPercentage)
    {
        if (passingPercentage is < 0 or > 100)
        {
            throw new ArgumentException("Passing percentage must be between 0 and 100.");
        }
    }

    private static bool IsCollegeAdmin(string requesterRole)
        => string.Equals(requesterRole?.Trim(), "CollegeAdmin", StringComparison.OrdinalIgnoreCase);

    private static bool IsSuperAdmin(string requesterRole)
        => string.Equals(requesterRole?.Trim(), "SuperAdmin", StringComparison.OrdinalIgnoreCase);

    private static bool IsTrainer(string requesterRole)
        => string.Equals(requesterRole?.Trim(), "Trainer", StringComparison.OrdinalIgnoreCase);

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

    private static AssessmentStatus? NormalizeAssessmentStatusFilter(string? assessmentStatus)
    {
        if (string.IsNullOrWhiteSpace(assessmentStatus))
        {
            return null;
        }

        var normalizedStatus = assessmentStatus
            .Trim()
            .Replace(" ", "_")
            .ToUpperInvariant();

        return normalizedStatus switch
        {
            "DRAFT" => AssessmentStatus.Draft,
            "SCHEDULED" => AssessmentStatus.Scheduled,
            "LIVE" => AssessmentStatus.Live,
            "COMPLETED" => AssessmentStatus.Completed,
            "CANCELLED" => AssessmentStatus.Cancelled,
            _ => throw new ArgumentException($"Unsupported assessment status filter '{assessmentStatus}'.")
        };
    }
}
