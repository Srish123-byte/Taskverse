using Microsoft.EntityFrameworkCore;
using Taskverse.Data.Enums;
using Taskverse.API.College.Service.DTOs;
using Taskverse.Data.DataAccess;
using Taskverse.API.College.Service.Services;

namespace Taskverse.API.College.Service.Orchestrators;

public class CollegeOrchestrator : ICollegeOrchestrator
{
    private const int AttendanceEditWindowMinutes = 30;
    private const string CollegeAdminAttendanceSubmitterName = "College Admin";
    private readonly TaskverseContext _context;
    private readonly IAttendanceExportService _attendanceExportService;

    private sealed record AttendanceAccessContext(
        bool IsCollegeAdmin,
        Guid? TrainerId);

    public CollegeOrchestrator(TaskverseContext context, IAttendanceExportService attendanceExportService)
    {
        _context = context;
        _attendanceExportService = attendanceExportService;
    }

    public Task<List<PendingUserDto>> GetPendingUsersByCollege(Guid collegeId) =>
        _context.Users
            .AsNoTracking()
            .Where(user =>
                user.Status == UserStatus.PENDING_APPROVAL &&
                user.CollegeId == collegeId &&
                user.Role.Trim().ToLower() != "collegeadmin" &&
                user.Role.Trim().ToLower() != "superadmin")
            .OrderBy(user => user.CreatedAt)
            .Select(user => new PendingUserDto
            {
                UserId = user.Id.ToString(),
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                Status = user.Status.ToString(),
                CreatedAt = user.CreatedAt,
                InstitutionName = user.CollegeName
            })
            .ToListAsync();

    public Task<List<ApprovedTrainerDto>> GetApprovedTrainersByCollege(Guid collegeId) =>
        _context.Trainers
            .AsNoTracking()
            .Where(trainer => trainer.CollegeId == collegeId && trainer.Status == UserStatus.APPROVED)
            .OrderBy(trainer => trainer.FullName)
            .ThenBy(trainer => trainer.Email)
            .Select(trainer => new ApprovedTrainerDto
            {
                TrainerId = trainer.TrainerId.ToString(),
                UserId = trainer.UserId.ToString(),
                FullName = trainer.FullName,
                Email = trainer.Email
            })
            .ToListAsync();

    public Task<List<ApprovedStudentDto>> GetApprovedUnassignedStudentsByCollege(Guid collegeId) =>
        _context.Students
            .AsNoTracking()
            .Where(student =>
                student.CollegeId == collegeId &&
                student.Status == UserStatus.APPROVED &&
                !student.ClassId.HasValue &&
                !student.BatchId.HasValue)
            .OrderBy(student => student.FullName)
            .ThenBy(student => student.Email)
            .Select(student => new ApprovedStudentDto
            {
                StudentId = student.StudentId.ToString(),
                UserId = student.UserId.ToString(),
                FullName = student.FullName,
                Email = student.Email
            })
            .ToListAsync();

    public Task<List<SubjectOptionDto>> GetSubjects() =>
        _context.Subjects
            .AsNoTracking()
            .Where(subject => subject.IsActive)
            .OrderBy(subject => subject.SubjectName)
            .Select(subject => new SubjectOptionDto
            {
                SubjectId = subject.SubjectId.ToString(),
                SubjectName = subject.SubjectName
            })
            .ToListAsync();

    public async Task<List<AttendanceBatchGroupDto>> GetAttendanceBatchGroups(Guid collegeId, Guid requesterUserId)
    {
        var accessContext = await EnsureAttendanceAccess(collegeId, requesterUserId);

        var batches = await _context.Batches
            .AsNoTracking()
            .Where(item =>
                item.CollegeId == collegeId &&
                (accessContext.IsCollegeAdmin ||
                 item.TrainerBatches.Any(trainerBatch => trainerBatch.TrainerId == accessContext.TrainerId)))
            .OrderBy(item => item.Class.Name)
            .ThenBy(item => item.Class.AcademicYear)
            .ThenBy(item => item.Name)
            .Select(item => new
            {
                item.BatchId,
                item.Name,
                item.ClassId,
                ClassName = item.Class.Name,
                item.Class.AcademicYear
            })
            .ToListAsync();

        var ownerNames = await GetCurrentBatchOwnerNames(batches.Select(item => item.BatchId));

        return batches
            .GroupBy(item => new { item.ClassId, item.ClassName, item.AcademicYear })
            .Select(group => new AttendanceBatchGroupDto
            {
                ClassId = group.Key.ClassId.ToString(),
                ClassName = group.Key.ClassName,
                AcademicYear = group.Key.AcademicYear,
                Batches = group
                    .Select(item => new AttendanceBatchOptionDto
                    {
                        BatchId = item.BatchId.ToString(),
                        BatchName = item.Name,
                        BatchOwnerTrainerName = ownerNames.TryGetValue(item.BatchId, out var ownerName)
                            ? ownerName
                            : null
                    })
                    .ToList()
            })
            .ToList();
    }

    public async Task<AttendanceRosterDto> GetAttendanceRoster(AttendanceRosterRequestDto dto)
    {
        ValidateAttendanceSession(dto.AttendanceSession);

        var accessContext = await EnsureAttendanceAccess(dto.CollegeId, dto.RequesterUserId);
        var normalizedDate = dto.AttendanceDate.Date;

        var batch = await _context.Batches
            .AsNoTracking()
            .Where(item => item.BatchId == dto.BatchId && item.CollegeId == dto.CollegeId)
            .Select(item => new
            {
                item.BatchId,
                item.Name,
                item.ClassId,
                ClassName = item.Class.Name,
                item.Class.AcademicYear
            })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Selected batch was not found for this college.");

        await EnsureAttendanceBatchAccess(accessContext, dto.BatchId);

        var students = await _context.Students
            .AsNoTracking()
            .Where(item =>
                item.CollegeId == dto.CollegeId &&
                item.BatchId == dto.BatchId &&
                item.Status == UserStatus.APPROVED)
            .OrderBy(item => item.FullName)
            .ThenBy(item => item.Email)
            .Select(item => new AttendanceStudentDto
            {
                StudentId = item.StudentId.ToString(),
                UserId = item.UserId.ToString(),
                FullName = item.FullName,
                Email = item.Email,
                EnrollmentNumber = item.EnrollmentNumber
            })
            .ToListAsync();

        var existingSession = await _context.AttendanceSessions
            .AsNoTracking()
            .Include(item => item.Entries)
            .Include(item => item.SubmittedByTrainer)
            .Include(item => item.BatchOwnerTrainer)
            .FirstOrDefaultAsync(item =>
                item.BatchId == dto.BatchId &&
                item.AttendanceDate == normalizedDate &&
                item.AttendanceSessionType == dto.AttendanceSession);

        Dictionary<Guid, AttendanceEntryType> savedEntries = [];
        if (existingSession is not null)
        {
            savedEntries = existingSession.Entries
                .Where(item => item.StudentId.HasValue)
                .ToDictionary(item => item.StudentId!.Value, item => item.AttendanceEntryType);
        }

        foreach (var student in students)
        {
            if (Guid.TryParse(student.StudentId, out var studentId) &&
                savedEntries.TryGetValue(studentId, out var attendanceEntry))
            {
                student.AttendanceEntry = attendanceEntry;
            }
        }

        var presentCount = students.Count(item => item.AttendanceEntry == AttendanceEntryType.Present);
        var absentCount = students.Count(item => item.AttendanceEntry == AttendanceEntryType.Absent);
        var totalStudents = students.Count;
        var isLocked = existingSession is not null && IsAttendanceLocked(existingSession.SubmittedAt);
        var canEdit = existingSession is null ||
                      (!isLocked && (accessContext.IsCollegeAdmin || existingSession.SubmittedByTrainerId == accessContext.TrainerId));

        string? currentBatchOwnerName = null;
        var ownerNames = await GetCurrentBatchOwnerNames([batch.BatchId]);
        if (ownerNames.TryGetValue(batch.BatchId, out var ownerName))
        {
            currentBatchOwnerName = ownerName;
        }

        return new AttendanceRosterDto
        {
            ClassId = batch.ClassId.ToString(),
            ClassName = batch.ClassName,
            AcademicYear = batch.AcademicYear,
            BatchId = batch.BatchId.ToString(),
            BatchName = batch.Name,
            AttendanceDate = normalizedDate,
            AttendanceSession = dto.AttendanceSession,
            IsSubmitted = existingSession is not null,
            IsLocked = isLocked,
            CanEdit = canEdit,
            SubmittedByTrainerName = ResolveAttendanceSubmitterName(existingSession?.SubmittedByTrainer?.FullName),
            BatchOwnerTrainerName = existingSession?.BatchOwnerTrainer?.FullName ?? currentBatchOwnerName,
            SubmittedAt = existingSession?.SubmittedAt,
            LastModifiedAt = existingSession?.LastModifiedAt,
            TotalStudents = totalStudents,
            PresentCount = presentCount,
            AbsentCount = absentCount,
            AttendancePercentage = totalStudents == 0
                ? 0
                : Math.Round(presentCount * 100m / totalStudents, 2),
            Students = students
        };
    }

    public async Task<AttendanceRosterDto> SubmitAttendance(SubmitAttendanceDto dto)
    {
        ValidateAttendanceSession(dto.AttendanceSession);
        ValidateAttendanceEntries(dto.Entries);

        var accessContext = await EnsureAttendanceAccess(dto.CollegeId, dto.RequesterUserId);
        var normalizedDate = dto.AttendanceDate.Date;

        var batch = await _context.Batches
            .AsNoTracking()
            .Where(item => item.BatchId == dto.BatchId && item.CollegeId == dto.CollegeId)
            .Select(item => new { item.BatchId, item.ClassId })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Selected batch was not found for this college.");

        await EnsureAttendanceBatchAccess(accessContext, dto.BatchId);

        var activeStudents = await _context.Students
            .Where(item =>
                item.CollegeId == dto.CollegeId &&
                item.BatchId == dto.BatchId &&
                item.Status == UserStatus.APPROVED)
            .Select(item => item.StudentId)
            .ToListAsync();

        var activeStudentIds = activeStudents.ToHashSet();
        var submittedStudentIds = dto.Entries.Select(item => item.StudentId).ToList();

        if (submittedStudentIds.Count != submittedStudentIds.Distinct().Count())
        {
            throw new InvalidOperationException("Duplicate students were submitted in the attendance payload.");
        }

        if (submittedStudentIds.Count != activeStudentIds.Count || submittedStudentIds.Any(id => !activeStudentIds.Contains(id)))
        {
            throw new InvalidOperationException("Attendance can only be submitted for the current live batch roster.");
        }

        var existingSession = await _context.AttendanceSessions
            .Include(item => item.Entries)
            .FirstOrDefaultAsync(item =>
                item.BatchId == dto.BatchId &&
                item.AttendanceDate == normalizedDate &&
                item.AttendanceSessionType == dto.AttendanceSession);

        var now = DateTime.UtcNow;
        if (existingSession is null)
        {
            existingSession = new AttendanceSession
            {
                AttendanceSessionId = Guid.NewGuid(),
                CollegeId = dto.CollegeId,
                ClassId = batch.ClassId,
                BatchId = dto.BatchId,
                AttendanceDate = normalizedDate,
                AttendanceSessionType = dto.AttendanceSession,
                SubmittedByTrainerId = accessContext.TrainerId,
                BatchOwnerTrainerId = await ResolveBatchOwnerTrainerId(dto.BatchId),
                SubmittedAt = now,
                LastModifiedAt = now,
                Entries = dto.Entries.Select(item => new AttendanceEntryRecord
                {
                    AttendanceEntryId = Guid.NewGuid(),
                    StudentId = item.StudentId,
                    AttendanceEntryType = item.AttendanceEntry
                }).ToList()
            };

            _context.AttendanceSessions.Add(existingSession);
        }
        else
        {
            if (!accessContext.IsCollegeAdmin && existingSession.SubmittedByTrainerId != accessContext.TrainerId)
            {
                throw new InvalidOperationException("This attendance session was already submitted by another trainer.");
            }

            if (IsAttendanceLocked(existingSession.SubmittedAt))
            {
                throw new InvalidOperationException("This attendance session is locked and can no longer be edited.");
            }

            existingSession.LastModifiedAt = now;
            existingSession.BatchOwnerTrainerId ??= await ResolveBatchOwnerTrainerId(dto.BatchId);

            var entryLookup = existingSession.Entries
                .Where(item => item.StudentId.HasValue)
                .ToDictionary(item => item.StudentId!.Value);
            foreach (var item in dto.Entries)
            {
                if (entryLookup.TryGetValue(item.StudentId, out var existingEntry))
                {
                    existingEntry.AttendanceEntryType = item.AttendanceEntry;
                }
                else
                {
                    existingSession.Entries.Add(new AttendanceEntryRecord
                    {
                        AttendanceEntryId = Guid.NewGuid(),
                        AttendanceSessionId = existingSession.AttendanceSessionId,
                        StudentId = item.StudentId,
                        AttendanceEntryType = item.AttendanceEntry
                    });
                }
            }

            var entriesToRemove = existingSession.Entries
                .Where(item => !item.StudentId.HasValue || !activeStudentIds.Contains(item.StudentId.Value))
                .ToList();
            if (entriesToRemove.Count > 0)
            {
                _context.AttendanceEntries.RemoveRange(entriesToRemove);
            }
        }

        await _context.SaveChangesAsync();

        return await GetAttendanceRoster(new AttendanceRosterRequestDto
        {
            CollegeId = dto.CollegeId,
            RequesterUserId = dto.RequesterUserId,
            BatchId = dto.BatchId,
            AttendanceDate = normalizedDate,
            AttendanceSession = dto.AttendanceSession
        });
    }

    public async Task<AttendanceHistoryDto> GetAttendanceHistory(AttendanceHistoryRequestDto dto)
    {
        var accessContext = await EnsureAttendanceAccess(dto.CollegeId, dto.RequesterUserId);
        ValidateDateRange(dto.FromDate, dto.ToDate);

        var batch = await _context.Batches
            .AsNoTracking()
            .Where(item => item.BatchId == dto.BatchId && item.CollegeId == dto.CollegeId)
            .Select(item => new { item.BatchId, item.Name })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Selected batch was not found for this college.");

        await EnsureAttendanceBatchAccess(accessContext, dto.BatchId);

        var items = await BuildHistoryItems(dto.BatchId, dto.FromDate.Date, dto.ToDate.Date, useLiveRecalculatedTotals: false);

        return new AttendanceHistoryDto
        {
            BatchId = batch.BatchId.ToString(),
            BatchName = batch.Name,
            FromDate = dto.FromDate.Date,
            ToDate = dto.ToDate.Date,
            Items = items
        };
    }

    public async Task<AttendanceExportDto> ExportAttendance(AttendanceHistoryRequestDto dto)
    {
        var accessContext = await EnsureAttendanceAccess(dto.CollegeId, dto.RequesterUserId);
        ValidateDateRange(dto.FromDate, dto.ToDate);

        var batch = await _context.Batches
            .AsNoTracking()
            .Where(item => item.BatchId == dto.BatchId && item.CollegeId == dto.CollegeId)
            .Select(item => new { item.BatchId, item.Name })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Selected batch was not found for this college.");

        await EnsureAttendanceBatchAccess(accessContext, dto.BatchId);

        var summaryItems = await BuildHistoryItems(dto.BatchId, dto.FromDate.Date, dto.ToDate.Date, useLiveRecalculatedTotals: true);
        var entryItems = await BuildExportEntries(dto.BatchId, dto.FromDate.Date, dto.ToDate.Date);
        var exportArtifact = _attendanceExportService.BuildWorkbook(batch.Name, dto.FromDate.Date, dto.ToDate.Date, summaryItems, entryItems);

        return new AttendanceExportDto
        {
            FileName = exportArtifact.FileName,
            ContentType = exportArtifact.ContentType,
            ContentBase64 = Convert.ToBase64String(exportArtifact.Content),
            BatchId = batch.BatchId.ToString(),
            BatchName = batch.Name,
            FromDate = dto.FromDate.Date,
            ToDate = dto.ToDate.Date,
            Entries = entryItems
        };
    }

    public async Task<List<PendingUserDto>> GetPendingUsersForCollegeAdmin(Guid collegeAdminUserId)
    {
        var collegeAdmin = await _context.Users
            .AsNoTracking()
            .Where(user => user.Id == collegeAdminUserId)
            .Select(user => new
            {
                user.Id,
                user.CollegeId
            })
            .FirstOrDefaultAsync();

        if (collegeAdmin is null)
        {
            throw new KeyNotFoundException($"College admin user not found for userId={collegeAdminUserId}.");
        }

        if (!collegeAdmin.CollegeId.HasValue)
        {
            throw new InvalidOperationException($"College admin user '{collegeAdminUserId}' is not mapped to a college.");
        }

        return await GetPendingUsersByCollege(collegeAdmin.CollegeId.Value);
    }

    public async Task<CollegeClassSummaryDto> CreateClass(Guid collegeId, CreateCollegeClassDto dto)
    {
        var name = NormalizeRequired(dto.Name, "Class name");
        var academicYear = NormalizeOptional(dto.AcademicYear);
        var department = NormalizeOptional(dto.Department);

        var exists = await _context.Classes.AnyAsync(item =>
            item.CollegeId == collegeId &&
            item.Name == name &&
            item.AcademicYear == academicYear);

        if (exists)
        {
            throw new InvalidOperationException($"A class named '{name}' already exists for academic year '{academicYear ?? "N/A"}'.");
        }

        var entity = new Class
        {
            ClassId = Guid.NewGuid(),
            CollegeId = collegeId,
            Name = name,
            AcademicYear = academicYear,
            Description = department,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        _context.Classes.Add(entity);
        await _context.SaveChangesAsync();

        return new CollegeClassSummaryDto
        {
            ClassId = entity.ClassId.ToString(),
            CollegeId = entity.CollegeId.ToString(),
            Name = entity.Name,
            AcademicYear = entity.AcademicYear,
            Department = entity.Description,
            TotalStudents = 0,
            TotalCapacity = 0,
            CreatedAt = entity.CreatedAt,
            Batches = []
        };
    }

    public async Task<CollegeClassSummaryDto> UpdateClass(Guid collegeId, Guid classId, UpdateCollegeClassDto dto)
    {
        var name = NormalizeRequired(dto.Name, "Class name");
        var academicYear = NormalizeOptional(dto.AcademicYear);
        var department = NormalizeOptional(dto.Department);

        var entity = await _context.Classes
            .FirstOrDefaultAsync(item => item.ClassId == classId && item.CollegeId == collegeId);

        if (entity is null)
        {
            throw new KeyNotFoundException($"Class '{classId}' was not found for this college.");
        }

        var exists = await _context.Classes.AnyAsync(item =>
            item.CollegeId == collegeId &&
            item.ClassId != classId &&
            item.Name == name &&
            item.AcademicYear == academicYear);

        if (exists)
        {
            throw new InvalidOperationException($"A class named '{name}' already exists for academic year '{academicYear ?? "N/A"}'.");
        }

        entity.Name = name;
        entity.AcademicYear = academicYear;
        entity.Description = department;
        entity.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var totals = await _context.Batches
            .AsNoTracking()
            .Where(item => item.ClassId == entity.ClassId && item.CollegeId == collegeId)
            .GroupJoin(
                _context.Students.AsNoTracking().Where(student => student.CollegeId == collegeId && student.Status == UserStatus.APPROVED),
                batch => batch.BatchId,
                student => student.BatchId ?? Guid.Empty,
                (batch, students) => new
                {
                    Capacity = batch.Capacity ?? 0,
                    StudentCount = students.Count()
                })
            .ToListAsync();

        return new CollegeClassSummaryDto
        {
            ClassId = entity.ClassId.ToString(),
            CollegeId = entity.CollegeId.ToString(),
            Name = entity.Name,
            AcademicYear = entity.AcademicYear,
            Department = entity.Description,
            TotalStudents = totals.Sum(item => item.StudentCount),
            TotalCapacity = totals.Sum(item => item.Capacity),
            CreatedAt = entity.CreatedAt,
            Batches = []
        };
    }

    public async Task<CollegeBatchSummaryDto> CreateBatch(Guid collegeId, Guid classId, CreateCollegeBatchDto dto)
    {
        var name = NormalizeRequired(dto.Name, "Batch name");
        var description = NormalizeOptional(dto.Description);
        var requestedSubjectName = NormalizeOptional(dto.SubjectName);
        var capacity = dto.Capacity.GetValueOrDefault();
        if (capacity < 0)
        {
            throw new InvalidOperationException("Batch capacity cannot be negative.");
        }

        var subject = await ResolveSubject(dto.SubjectId, requestedSubjectName);

        var classExists = await _context.Classes
            .AsNoTracking()
            .AnyAsync(item => item.ClassId == classId && item.CollegeId == collegeId);

        if (!classExists)
        {
            throw new KeyNotFoundException($"Class '{classId}' was not found for this college.");
        }

        var exists = await _context.Batches.AnyAsync(item => item.ClassId == classId && item.Name == name);
        if (exists)
        {
            throw new InvalidOperationException($"A batch named '{name}' already exists for this class.");
        }

        var entity = new Batch
        {
            BatchId = Guid.NewGuid(),
            ClassId = classId,
            CollegeId = collegeId,
            Name = name,
            Description = description,
            Capacity = capacity,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        _context.Batches.Add(entity);
        _context.SubjectBatches.Add(new SubjectBatch
        {
            SubjectBatchId = Guid.NewGuid(),
            SubjectId = subject.SubjectId,
            BatchId = entity.BatchId,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return await BuildBatchSummary(entity);
    }

    public async Task<CollegeBatchSummaryDto> UpdateBatch(Guid collegeId, Guid classId, Guid batchId, UpdateCollegeBatchDto dto)
    {
        var name = NormalizeRequired(dto.Name, "Batch name");
        var description = NormalizeOptional(dto.Description);
        var requestedSubjectName = NormalizeOptional(dto.SubjectName);
        var capacity = dto.Capacity.GetValueOrDefault();
        if (capacity < 0)
        {
            throw new InvalidOperationException("Batch capacity cannot be negative.");
        }

        var batch = await _context.Batches
            .FirstOrDefaultAsync(item =>
                item.BatchId == batchId &&
                item.ClassId == classId &&
                item.CollegeId == collegeId);

        if (batch is null)
        {
            throw new KeyNotFoundException($"Batch '{batchId}' was not found for class '{classId}' in this college.");
        }

        var classExists = await _context.Classes
            .AsNoTracking()
            .AnyAsync(item => item.ClassId == classId && item.CollegeId == collegeId);

        if (!classExists)
        {
            throw new KeyNotFoundException($"Class '{classId}' was not found for this college.");
        }

        var exists = await _context.Batches.AnyAsync(item =>
            item.ClassId == classId &&
            item.BatchId != batchId &&
            item.Name == name);
        if (exists)
        {
            throw new InvalidOperationException($"A batch named '{name}' already exists for this class.");
        }

        var subject = await ResolveSubject(dto.SubjectId, requestedSubjectName);

        batch.Name = name;
        batch.Description = description;
        batch.Capacity = capacity;
        batch.ModifiedAt = DateTime.UtcNow;

        var existingSubjectMappings = await _context.SubjectBatches
            .Where(item => item.BatchId == batchId)
            .ToListAsync();

        if (existingSubjectMappings.Count > 0)
        {
            _context.SubjectBatches.RemoveRange(existingSubjectMappings);
        }

        _context.SubjectBatches.Add(new SubjectBatch
        {
            SubjectBatchId = Guid.NewGuid(),
            SubjectId = subject.SubjectId,
            BatchId = batch.BatchId,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return await BuildBatchSummary(batch);
    }

    public async Task<CollegeBatchSummaryDto> AssignBatchTrainers(Guid collegeId, Guid classId, Guid batchId, AssignBatchTrainersDto dto)
    {
        var batch = await _context.Batches
            .FirstOrDefaultAsync(item =>
                item.BatchId == batchId &&
                item.ClassId == classId &&
                item.CollegeId == collegeId);

        if (batch is null)
        {
            throw new KeyNotFoundException($"Batch '{batchId}' was not found for class '{classId}' in this college.");
        }

        var requestedTrainerIds = (dto.TrainerIds ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id =>
            {
                if (!Guid.TryParse(id, out var parsedId))
                {
                    throw new InvalidOperationException($"Trainer id '{id}' is invalid.");
                }

                return parsedId;
            })
            .Distinct()
            .ToList();

        var approvedTrainers = await _context.Trainers
            .Where(trainer =>
                trainer.CollegeId == collegeId &&
                trainer.Status == UserStatus.APPROVED &&
                requestedTrainerIds.Contains(trainer.TrainerId))
            .ToListAsync();

        if (approvedTrainers.Count != requestedTrainerIds.Count)
        {
            var approvedTrainerIds = approvedTrainers.Select(trainer => trainer.TrainerId).ToHashSet();
            var missingTrainerIds = requestedTrainerIds
                .Where(trainerId => !approvedTrainerIds.Contains(trainerId))
                .Select(trainerId => trainerId.ToString())
                .ToList();

            throw new InvalidOperationException($"Some selected trainers are not approved for this college: {string.Join(", ", missingTrainerIds)}.");
        }

        var existingBatchAssignments = await _context.TrainerBatches
            .Where(item => item.BatchId == batchId)
            .ToListAsync();

        var existingBatchTrainerIds = existingBatchAssignments
            .Select(item => item.TrainerId)
            .ToHashSet();

        var requestedTrainerIdSet = requestedTrainerIds.ToHashSet();
        var trainerBatchAssignmentsToRemove = existingBatchAssignments
            .Where(item => !requestedTrainerIdSet.Contains(item.TrainerId))
            .ToList();

        if (trainerBatchAssignmentsToRemove.Count > 0)
        {
            _context.TrainerBatches.RemoveRange(trainerBatchAssignmentsToRemove);
        }

        var trainerBatchAssignmentsToAdd = requestedTrainerIds
            .Where(trainerId => !existingBatchTrainerIds.Contains(trainerId))
            .Select(trainerId => new TrainerBatch
            {
                TrainerId = trainerId,
                BatchId = batchId
            })
            .ToList();

        if (trainerBatchAssignmentsToAdd.Count > 0)
        {
            _context.TrainerBatches.AddRange(trainerBatchAssignmentsToAdd);
        }

        var existingClassAssignments = await _context.TrainerClasses
            .Where(item => item.ClassId == classId)
            .ToListAsync();

        var existingClassTrainerIds = existingClassAssignments
            .Select(item => item.TrainerId)
            .ToHashSet();

        var trainerClassAssignmentsToAdd = requestedTrainerIds
            .Where(trainerId => !existingClassTrainerIds.Contains(trainerId))
            .Select(trainerId => new TrainerClass
            {
                TrainerId = trainerId,
                ClassId = classId
            })
            .ToList();

        if (trainerClassAssignmentsToAdd.Count > 0)
        {
            _context.TrainerClasses.AddRange(trainerClassAssignmentsToAdd);
        }

        var removedTrainerIds = trainerBatchAssignmentsToRemove
            .Select(item => item.TrainerId)
            .Distinct()
            .ToList();

        if (removedTrainerIds.Count > 0)
        {
            var remainingAssignedTrainerIdsForClass = await _context.TrainerBatches
                .Where(item =>
                    item.Batch.ClassId == classId &&
                    removedTrainerIds.Contains(item.TrainerId) &&
                    item.BatchId != batchId)
                .Select(item => item.TrainerId)
                .Distinct()
                .ToListAsync();

            var trainerIdsToRemoveFromClass = removedTrainerIds
                .Except(remainingAssignedTrainerIdsForClass)
                .ToList();

            if (trainerIdsToRemoveFromClass.Count > 0)
            {
                var classAssignmentsToRemove = existingClassAssignments
                    .Where(item => trainerIdsToRemoveFromClass.Contains(item.TrainerId))
                    .ToList();

                if (classAssignmentsToRemove.Count > 0)
                {
                    _context.TrainerClasses.RemoveRange(classAssignmentsToRemove);
                }
            }
        }

        batch.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await BuildBatchSummary(batch);
    }

    public async Task<CollegeBatchSummaryDto> AssignStudentToBatch(Guid collegeId, Guid classId, Guid batchId, AssignStudentToBatchDto dto)
    {
        var batch = await _context.Batches
            .FirstOrDefaultAsync(item =>
                item.BatchId == batchId &&
                item.ClassId == classId &&
                item.CollegeId == collegeId);

        if (batch is null)
        {
            throw new KeyNotFoundException($"Batch '{batchId}' was not found for class '{classId}' in this college.");
        }

        var requestedStudentIds = (dto.StudentIds ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id =>
            {
                if (!Guid.TryParse(id, out var parsedId))
                {
                    throw new InvalidOperationException($"Student id '{id}' is invalid.");
                }

                return parsedId;
            })
            .Distinct()
            .ToList();

        var requestedStudentIdSet = requestedStudentIds.ToHashSet();

        var existingBatchStudents = await _context.Students
            .Where(item =>
                item.CollegeId == collegeId &&
                item.ClassId == classId &&
                item.BatchId == batchId)
            .ToListAsync();

        var existingBatchStudentIds = existingBatchStudents
            .Select(item => item.StudentId)
            .ToHashSet();

        var studentsToAssignIds = requestedStudentIds
            .Where(studentId => !existingBatchStudentIds.Contains(studentId))
            .ToList();

        var studentsToUnassign = existingBatchStudents
            .Where(item => !requestedStudentIdSet.Contains(item.StudentId))
            .ToList();

        var studentsToAssign = studentsToAssignIds.Count == 0
            ? []
            : await _context.Students
                .Where(item => item.CollegeId == collegeId && studentsToAssignIds.Contains(item.StudentId))
                .ToListAsync();

        if (studentsToAssign.Count != studentsToAssignIds.Count)
        {
            var foundStudentIds = studentsToAssign.Select(item => item.StudentId).ToHashSet();
            var missingStudentIds = studentsToAssignIds
                .Where(studentId => !foundStudentIds.Contains(studentId))
                .Select(studentId => studentId.ToString())
                .ToList();

            throw new KeyNotFoundException($"Some selected students were not found for this college: {string.Join(", ", missingStudentIds)}.");
        }

        var unapprovedStudentIds = studentsToAssign
            .Where(item => item.Status != UserStatus.APPROVED)
            .Select(item => item.StudentId.ToString())
            .ToList();

        if (unapprovedStudentIds.Count > 0)
        {
            throw new InvalidOperationException($"Only approved students can be assigned. Invalid student ids: {string.Join(", ", unapprovedStudentIds)}.");
        }

        var conflictingStudents = studentsToAssign
            .Where(item =>
                (item.ClassId.HasValue || item.BatchId.HasValue) &&
                (item.ClassId != classId || item.BatchId != batchId))
            .Select(item => item.StudentId.ToString())
            .ToList();

        if (conflictingStudents.Count > 0)
        {
            throw new InvalidOperationException(
                $"Some selected students are already assigned to another class or batch: {string.Join(", ", conflictingStudents)}.");
        }

        var affectedUserIds = studentsToAssign
            .Select(item => item.UserId)
            .Concat(studentsToUnassign.Select(item => item.UserId))
            .Distinct()
            .ToList();

        var usersById = affectedUserIds.Count == 0
            ? new Dictionary<Guid, User>()
            : await _context.Users
                .Where(item => item.CollegeId == collegeId && affectedUserIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id);

        foreach (var student in studentsToAssign)
        {
            if (!usersById.TryGetValue(student.UserId, out var user))
            {
                throw new KeyNotFoundException($"User record was not found for student '{student.StudentId}'.");
            }

            if ((user.ClassId.HasValue || user.BatchId.HasValue) &&
                (user.ClassId != classId || user.BatchId != batchId))
            {
                throw new InvalidOperationException($"Student '{student.StudentId}' is already assigned to another class or batch.");
            }

            student.ClassId = classId;
            student.BatchId = batchId;
            student.ModifiedAt = DateTime.UtcNow;

            user.ClassId = classId;
            user.BatchId = batchId;
            user.ModifiedAt = DateTime.UtcNow;
        }

        foreach (var student in studentsToUnassign)
        {
            if (!usersById.TryGetValue(student.UserId, out var user))
            {
                throw new KeyNotFoundException($"User record was not found for student '{student.StudentId}'.");
            }

            student.ClassId = null;
            student.BatchId = null;
            student.ModifiedAt = DateTime.UtcNow;

            user.ClassId = null;
            user.BatchId = null;
            user.ModifiedAt = DateTime.UtcNow;
        }

        batch.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await BuildBatchSummary(batch);
    }

    public async Task DeleteClass(Guid collegeId, Guid classId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var classEntity = await _context.Classes
            .FirstOrDefaultAsync(item => item.ClassId == classId && item.CollegeId == collegeId);

        if (classEntity is null)
        {
            throw new KeyNotFoundException($"Class '{classId}' was not found for this college.");
        }

        var batchIds = await _context.Batches
            .Where(item => item.ClassId == classId && item.CollegeId == collegeId)
            .Select(item => item.BatchId)
            .ToListAsync();

        var affectedStudents = await _context.Students
            .Where(student =>
                student.CollegeId == collegeId &&
                (student.ClassId == classId || (student.BatchId.HasValue && batchIds.Contains(student.BatchId.Value))))
            .ToListAsync();

        foreach (var student in affectedStudents)
        {
            if (student.ClassId == classId)
            {
                student.ClassId = null;
            }

            if (student.BatchId.HasValue && batchIds.Contains(student.BatchId.Value))
            {
                student.BatchId = null;
            }

            student.ModifiedAt = DateTime.UtcNow;
        }

        if (batchIds.Count > 0)
        {
            var trainerBatchMappings = await _context.TrainerBatches
                .Where(item => batchIds.Contains(item.BatchId))
                .ToListAsync();

            if (trainerBatchMappings.Count > 0)
            {
                _context.TrainerBatches.RemoveRange(trainerBatchMappings);
            }

            var subjectBatchMappings = await _context.SubjectBatches
                .Where(item => batchIds.Contains(item.BatchId))
                .ToListAsync();

            if (subjectBatchMappings.Count > 0)
            {
                _context.SubjectBatches.RemoveRange(subjectBatchMappings);
            }

            var batches = await _context.Batches
                .Where(item => batchIds.Contains(item.BatchId))
                .ToListAsync();

            if (batches.Count > 0)
            {
                _context.Batches.RemoveRange(batches);
            }
        }

        var trainerClassMappings = await _context.TrainerClasses
            .Where(item => item.ClassId == classId)
            .ToListAsync();

        if (trainerClassMappings.Count > 0)
        {
            _context.TrainerClasses.RemoveRange(trainerClassMappings);
        }

        _context.Classes.Remove(classEntity);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task DeleteBatch(Guid collegeId, Guid classId, Guid batchId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var batch = await _context.Batches
            .FirstOrDefaultAsync(item =>
                item.BatchId == batchId &&
                item.ClassId == classId &&
                item.CollegeId == collegeId);

        if (batch is null)
        {
            throw new KeyNotFoundException($"Batch '{batchId}' was not found for class '{classId}' in this college.");
        }

        var studentsInBatch = await _context.Students
            .Where(student => student.CollegeId == collegeId && student.BatchId == batchId)
            .ToListAsync();

        foreach (var student in studentsInBatch)
        {
            student.BatchId = null;
            student.ModifiedAt = DateTime.UtcNow;
        }

        var trainerBatchMappings = await _context.TrainerBatches
            .Where(item => item.BatchId == batchId)
            .ToListAsync();

        var removedTrainerIds = trainerBatchMappings
            .Select(item => item.TrainerId)
            .Distinct()
            .ToList();

        if (trainerBatchMappings.Count > 0)
        {
            _context.TrainerBatches.RemoveRange(trainerBatchMappings);
        }

        var subjectBatchMappings = await _context.SubjectBatches
            .Where(item => item.BatchId == batchId)
            .ToListAsync();

        if (subjectBatchMappings.Count > 0)
        {
            _context.SubjectBatches.RemoveRange(subjectBatchMappings);
        }

        if (removedTrainerIds.Count > 0)
        {
            var remainingTrainerIds = await _context.TrainerBatches
                .Where(item => item.Batch.ClassId == classId && item.BatchId != batchId)
                .Select(item => item.TrainerId)
                .Distinct()
                .ToListAsync();

            var trainerIdsToRemoveFromClass = removedTrainerIds
                .Except(remainingTrainerIds)
                .ToList();

            if (trainerIdsToRemoveFromClass.Count > 0)
            {
                var trainerClassMappings = await _context.TrainerClasses
                    .Where(item => item.ClassId == classId && trainerIdsToRemoveFromClass.Contains(item.TrainerId))
                    .ToListAsync();

                if (trainerClassMappings.Count > 0)
                {
                    _context.TrainerClasses.RemoveRange(trainerClassMappings);
                }
            }
        }

        _context.Batches.Remove(batch);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task ApproveUser(Guid collegeId, string userId, CollegeUserActionDto dto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var user = await GetPendingUserEntity(collegeId, userId);
        if (user is null)
        {
            throw new KeyNotFoundException($"Pending user not found for userId={userId} in collegeId={collegeId}.");
        }

        var normalizedRole = NormalizeRole(user.Role);
        switch (normalizedRole)
        {
            case "student":
                await EnsureStudentApprovalRecord(user, dto.PerformedByUserId);
                break;
            case "trainer":
                await EnsureTrainerApprovalRecord(user, dto.PerformedByUserId);
                break;
            default:
                throw new InvalidOperationException($"Unsupported pending user role '{user.Role}' for college admin approval.");
        }

        user.Status = UserStatus.APPROVED;
        user.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task RejectUser(Guid collegeId, string userId, CollegeUserActionDto dto)
    {
        _ = dto;

        var user = await GetPendingUserEntity(collegeId, userId);
        if (user is null)
        {
            throw new KeyNotFoundException($"Pending user not found for userId={userId} in collegeId={collegeId}.");
        }

        var normalizedRole = NormalizeRole(user.Role);
        if (normalizedRole is not ("student" or "trainer"))
        {
            throw new InvalidOperationException($"Unsupported pending user role '{user.Role}' for college admin rejection.");
        }

        user.Status = UserStatus.REJECTED;
        user.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    private async Task<User?> GetPendingUserEntity(Guid collegeId, string userId)
    {
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return null;
        }

        return await _context.Users.FirstOrDefaultAsync(user =>
            user.Id == parsedUserId &&
            user.CollegeId == collegeId &&
            user.Status == UserStatus.PENDING_APPROVAL);
    }

    private async Task EnsureStudentApprovalRecord(User user, Guid? approvedByUserId)
    {
        if (!user.CollegeId.HasValue)
        {
            throw new InvalidOperationException($"Student user '{user.Id}' cannot be approved without a college.");
        }

        await EnsureUserCollegeName(user);

        var existingStudent = await _context.Students
            .FirstOrDefaultAsync(student => student.UserId == user.Id);

        if (existingStudent is not null)
        {
            existingStudent.ClassId = user.ClassId;
            existingStudent.BatchId = user.BatchId;
            existingStudent.EnrollmentNumber = string.IsNullOrWhiteSpace(user.EnrollmentNumber) ? null : user.EnrollmentNumber.Trim();
            existingStudent.Status = UserStatus.APPROVED;
            existingStudent.ModifiedAt = DateTime.UtcNow;
            existingStudent.ApprovedBy = approvedByUserId;
            return;
        }

        _context.Students.Add(new Student
        {
            StudentId = Guid.NewGuid(),
            UserId = user.Id,
            CollegeId = user.CollegeId.Value,
            ClassId = user.ClassId,
            BatchId = user.BatchId,       // nullable – allowed to be null
            EnrollmentNumber = string.IsNullOrWhiteSpace(user.EnrollmentNumber) ? null : user.EnrollmentNumber.Trim(),
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            Status = UserStatus.APPROVED,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            ApprovedBy = approvedByUserId
        });
    }

    private async Task EnsureTrainerApprovalRecord(User user, Guid? approvedByUserId)
    {
        if (!user.CollegeId.HasValue)
        {
            throw new InvalidOperationException($"Trainer user '{user.Id}' cannot be approved without a college.");
        }

        await EnsureUserCollegeName(user);

        var existingTrainer = await _context.Trainers
            .FirstOrDefaultAsync(trainer => trainer.UserId == user.Id);

        if (existingTrainer is not null)
        {
            existingTrainer.Status = UserStatus.APPROVED;
            existingTrainer.ModifiedAt = DateTime.UtcNow;
            existingTrainer.ApprovedBy = approvedByUserId;
            return;
        }

        _context.Trainers.Add(new Trainer
        {
            TrainerId = Guid.NewGuid(),
            UserId = user.Id,
            CollegeId = user.CollegeId.Value,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            Status = UserStatus.APPROVED,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            ApprovedBy = approvedByUserId
        });
    }

    private async Task EnsureUserCollegeName(User user)
    {
        if (!user.CollegeId.HasValue)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(user.CollegeName))
        {
            user.CollegeName = user.CollegeName.Trim();
            return;
        }

        var collegeName = await _context.Colleges
            .AsNoTracking()
            .Where(college => college.CollegeId == user.CollegeId.Value)
            .Select(college => college.CollegeName)
            .FirstOrDefaultAsync();

        user.CollegeName = string.IsNullOrWhiteSpace(collegeName)
            ? null
            : collegeName.Trim();
    }

    private async Task<CollegeBatchSummaryDto> BuildBatchSummary(Batch batch)
    {
        var studentCount = await _context.Students
            .AsNoTracking()
            .CountAsync(student =>
                student.BatchId == batch.BatchId &&
                student.CollegeId == batch.CollegeId &&
                student.Status == UserStatus.APPROVED);

        var assignedTrainers = await GetAssignedTrainersByBatch([batch.BatchId]);
        var assignedStudents = await GetAssignedStudentsByBatch([batch.BatchId]);
        var subject = await _context.SubjectBatches
            .AsNoTracking()
            .Where(item => item.BatchId == batch.BatchId)
            .Select(item => new
            {
                item.SubjectId,
                item.Subject.SubjectName
            })
            .OrderBy(item => item.SubjectName)
            .FirstOrDefaultAsync();

        return new CollegeBatchSummaryDto
        {
            BatchId = batch.BatchId.ToString(),
            ClassId = batch.ClassId.ToString(),
            CollegeId = batch.CollegeId.ToString(),
            Name = batch.Name,
            Description = batch.Description,
            SubjectId = subject?.SubjectId.ToString(),
            SubjectName = subject?.SubjectName,
            Capacity = batch.Capacity ?? 0,
            StudentCount = studentCount,
            CreatedAt = batch.CreatedAt,
            AssignedTrainers = assignedTrainers.TryGetValue(batch.BatchId, out var trainers)
                ? trainers
                : [],
            AssignedStudents = assignedStudents.TryGetValue(batch.BatchId, out var students)
                ? students
                : []
        };
    }

    private async Task<Subject> ResolveSubject(string? subjectId, string? subjectName)
    {
        Subject? subject = null;

        if (!string.IsNullOrWhiteSpace(subjectId))
        {
            if (!Guid.TryParse(subjectId, out var parsedSubjectId))
            {
                throw new InvalidOperationException("Subject id is invalid.");
            }

            subject = await _context.Subjects
                .FirstOrDefaultAsync(item => item.SubjectId == parsedSubjectId && item.IsActive);

            if (subject is null)
            {
                throw new KeyNotFoundException($"Subject '{subjectId}' was not found.");
            }
        }

        if (!string.IsNullOrWhiteSpace(subjectName))
        {
            var existingSubject = await _context.Subjects
                .FirstOrDefaultAsync(item => item.SubjectName.ToLower() == subjectName.ToLower());

            if (existingSubject is not null)
            {
                if (subject is not null && existingSubject.SubjectId != subject.SubjectId)
                {
                    throw new InvalidOperationException("Selected subject and new subject input do not match.");
                }

                if (!existingSubject.IsActive)
                {
                    existingSubject.IsActive = true;
                    existingSubject.ModifiedAt = DateTime.UtcNow;
                }

                subject = existingSubject;
            }
            else
            {
                if (subject is not null)
                {
                    throw new InvalidOperationException("Selected subject and new subject input do not match.");
                }

                subject = new Subject
                {
                    SubjectId = Guid.NewGuid(),
                    SubjectName = subjectName,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                };

                _context.Subjects.Add(subject);
            }
        }

        return subject ?? throw new InvalidOperationException("Subject is required.");
    }

    private async Task<Dictionary<Guid, List<ApprovedTrainerDto>>> GetAssignedTrainersByBatch(IEnumerable<Guid> batchIds)
    {
        var batchIdList = batchIds.Distinct().ToList();
        if (batchIdList.Count == 0)
        {
            return [];
        }

        var assignments = await _context.TrainerBatches
            .AsNoTracking()
            .Where(item => batchIdList.Contains(item.BatchId) && item.Trainer.Status == UserStatus.APPROVED)
            .Select(item => new
            {
                item.BatchId,
                item.Trainer.TrainerId,
                item.Trainer.UserId,
                item.Trainer.FullName,
                item.Trainer.Email
            })
            .ToListAsync();

        return assignments
            .GroupBy(item => item.BatchId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(item => item.FullName)
                    .ThenBy(item => item.Email)
                    .Select(item => new ApprovedTrainerDto
                    {
                        TrainerId = item.TrainerId.ToString(),
                        UserId = item.UserId.ToString(),
                        FullName = item.FullName,
                        Email = item.Email
                    })
                    .ToList());
    }

    private async Task<Dictionary<Guid, List<ApprovedStudentDto>>> GetAssignedStudentsByBatch(IEnumerable<Guid> batchIds)
    {
        var batchIdList = batchIds.Distinct().ToList();
        if (batchIdList.Count == 0)
        {
            return [];
        }

        var assignments = await _context.Students
            .AsNoTracking()
            .Where(item =>
                item.BatchId.HasValue &&
                batchIdList.Contains(item.BatchId.Value) &&
                item.Status == UserStatus.APPROVED)
            .Select(item => new
            {
                BatchId = item.BatchId!.Value,
                item.StudentId,
                item.UserId,
                item.FullName,
                item.Email
            })
            .ToListAsync();

        return assignments
            .GroupBy(item => item.BatchId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(item => item.FullName)
                    .ThenBy(item => item.Email)
                    .Select(item => new ApprovedStudentDto
                    {
                        StudentId = item.StudentId.ToString(),
                        UserId = item.UserId.ToString(),
                        FullName = item.FullName,
                        Email = item.Email
                    })
                    .ToList());
    }

    private async Task<AttendanceAccessContext> EnsureAttendanceAccess(Guid collegeId, Guid requesterUserId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Where(item => item.Id == requesterUserId)
            .Select(item => new
            {
                item.Id,
                item.CollegeId,
                item.Role,
                item.Status
            })
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Attendance access is not available for this user.");

        if (user.CollegeId != collegeId || user.Status != UserStatus.APPROVED)
        {
            throw new InvalidOperationException("Attendance access is not available for this user.");
        }

        var normalizedRole = NormalizeRole(user.Role);
        if (normalizedRole == "collegeadmin")
        {
            return new AttendanceAccessContext(
                IsCollegeAdmin: true,
                TrainerId: null);
        }

        if (normalizedRole == "trainer")
        {
            var trainer = await _context.Trainers
                .AsNoTracking()
                .Where(item =>
                    item.CollegeId == collegeId &&
                    item.UserId == requesterUserId &&
                    item.Status == UserStatus.APPROVED)
                .Select(item => new
                {
                    item.TrainerId
                })
                .FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("Trainer access is required for attendance.");

            return new AttendanceAccessContext(
                IsCollegeAdmin: false,
                TrainerId: trainer.TrainerId);
        }

        throw new InvalidOperationException("Attendance access is only available for college admins and trainers.");
    }

    private async Task EnsureAttendanceBatchAccess(AttendanceAccessContext accessContext, Guid batchId)
    {
        if (accessContext.IsCollegeAdmin)
        {
            return;
        }

        var hasAssignment = await _context.TrainerBatches
            .AsNoTracking()
            .AnyAsync(item => item.BatchId == batchId && item.TrainerId == accessContext.TrainerId);

        if (!hasAssignment)
        {
            throw new InvalidOperationException("This trainer is not assigned to the requested batch.");
        }
    }

    private static string ResolveAttendanceSubmitterName(string? submittedByTrainerName) =>
        string.IsNullOrWhiteSpace(submittedByTrainerName)
            ? CollegeAdminAttendanceSubmitterName
            : submittedByTrainerName;

    private async Task<Guid?> ResolveBatchOwnerTrainerId(Guid batchId)
    {
        var trainerIds = await _context.TrainerBatches
            .AsNoTracking()
            .Where(item => item.BatchId == batchId && item.Trainer.Status == UserStatus.APPROVED)
            .OrderBy(item => item.Trainer.FullName)
            .Select(item => item.TrainerId)
            .Distinct()
            .ToListAsync();

        return trainerIds.Count == 1 ? trainerIds[0] : null;
    }

    private async Task<Dictionary<Guid, string?>> GetCurrentBatchOwnerNames(IEnumerable<Guid> batchIds)
    {
        var batchIdList = batchIds.Distinct().ToList();
        if (batchIdList.Count == 0)
        {
            return [];
        }

        var assignments = await _context.TrainerBatches
            .AsNoTracking()
            .Where(item => batchIdList.Contains(item.BatchId) && item.Trainer.Status == UserStatus.APPROVED)
            .Select(item => new
            {
                item.BatchId,
                item.Trainer.FullName
            })
            .ToListAsync();

        return assignments
            .GroupBy(item => item.BatchId)
            .ToDictionary(
                group => group.Key,
                group => group.Count() == 1 ? group.First().FullName : null);
    }

    private async Task<List<AttendanceHistoryItemDto>> BuildHistoryItems(Guid batchId, DateTime fromDate, DateTime toDate, bool useLiveRecalculatedTotals)
    {
        var sessions = await _context.AttendanceSessions
            .AsNoTracking()
            .Include(item => item.Entries)
            .Include(item => item.SubmittedByTrainer)
            .Include(item => item.BatchOwnerTrainer)
            .Where(item =>
                item.BatchId == batchId &&
                item.AttendanceDate >= fromDate &&
                item.AttendanceDate <= toDate)
            .OrderByDescending(item => item.AttendanceDate)
            .ThenByDescending(item => item.AttendanceSessionType)
            .ToListAsync();

        HashSet<Guid>? liveStudentIds = null;
        int liveStudentCount = 0;
        if (useLiveRecalculatedTotals)
        {
            var currentStudentIds = await _context.Students
                .AsNoTracking()
                .Where(item => item.BatchId == batchId && item.Status == UserStatus.APPROVED)
                .Select(item => item.StudentId)
                .ToListAsync();

            liveStudentIds = currentStudentIds.ToHashSet();
            liveStudentCount = currentStudentIds.Count;
        }

        return sessions.Select(item =>
        {
            var relevantEntries = useLiveRecalculatedTotals
                ? item.Entries.Where(entry => entry.StudentId.HasValue && liveStudentIds!.Contains(entry.StudentId.Value)).ToList()
                : item.Entries.Where(entry => entry.StudentId.HasValue).ToList();

            var totalStudents = useLiveRecalculatedTotals ? liveStudentCount : relevantEntries.Count;
            var presentCount = relevantEntries.Count(entry => entry.AttendanceEntryType == AttendanceEntryType.Present);
            var absentCount = relevantEntries.Count(entry => entry.AttendanceEntryType == AttendanceEntryType.Absent);

            return new AttendanceHistoryItemDto
            {
                AttendanceSessionId = item.AttendanceSessionId.ToString(),
                AttendanceDate = item.AttendanceDate,
                AttendanceSession = item.AttendanceSessionType,
                SubmittedByTrainerName = ResolveAttendanceSubmitterName(item.SubmittedByTrainer?.FullName),
                BatchOwnerTrainerName = item.BatchOwnerTrainer?.FullName,
                SubmittedAt = item.SubmittedAt,
                LastModifiedAt = item.LastModifiedAt,
                IsLocked = IsAttendanceLocked(item.SubmittedAt),
                TotalStudents = totalStudents,
                PresentCount = presentCount,
                AbsentCount = absentCount,
                AttendancePercentage = totalStudents == 0
                    ? 0
                    : Math.Round(presentCount * 100m / totalStudents, 2)
            };
        }).ToList();
    }

    private async Task<List<AttendanceExportEntryDto>> BuildExportEntries(Guid batchId, DateTime fromDate, DateTime toDate)
    {
        var currentStudents = await _context.Students
            .AsNoTracking()
            .Where(item => item.BatchId == batchId && item.Status == UserStatus.APPROVED)
            .Select(item => new
            {
                item.StudentId,
                item.FullName,
                item.Email,
                item.EnrollmentNumber
            })
            .ToListAsync();

        var studentLookup = currentStudents.ToDictionary(item => item.StudentId);
        var liveStudentIds = currentStudents.Select(item => item.StudentId).ToHashSet();

        var sessions = await _context.AttendanceSessions
            .AsNoTracking()
            .Include(item => item.Entries)
            .Include(item => item.SubmittedByTrainer)
            .Include(item => item.BatchOwnerTrainer)
            .Where(item =>
                item.BatchId == batchId &&
                item.AttendanceDate >= fromDate &&
                item.AttendanceDate <= toDate)
            .OrderBy(item => item.AttendanceDate)
            .ThenBy(item => item.AttendanceSessionType)
            .ToListAsync();

        var exportEntries = new List<AttendanceExportEntryDto>();
        foreach (var session in sessions)
        {
            foreach (var entry in session.Entries.Where(item => item.StudentId.HasValue && liveStudentIds.Contains(item.StudentId.Value)))
            {
                var student = studentLookup[entry.StudentId!.Value];
                exportEntries.Add(new AttendanceExportEntryDto
                {
                    AttendanceSessionId = session.AttendanceSessionId.ToString(),
                    AttendanceDate = session.AttendanceDate,
                    AttendanceSession = session.AttendanceSessionType,
                    StudentName = student.FullName,
                    EnrollmentNumber = student.EnrollmentNumber,
                    Email = student.Email,
                    AttendanceEntry = entry.AttendanceEntryType,
                    SubmittedByTrainerName = ResolveAttendanceSubmitterName(session.SubmittedByTrainer?.FullName),
                    BatchOwnerTrainerName = session.BatchOwnerTrainer?.FullName,
                    SubmittedAt = session.SubmittedAt,
                    LastModifiedAt = session.LastModifiedAt
                });
            }
        }

        return exportEntries;
    }

    private static bool IsAttendanceLocked(DateTime submittedAt) =>
        DateTime.UtcNow > submittedAt.AddMinutes(AttendanceEditWindowMinutes);

    private static void ValidateAttendanceSession(AttendanceSessionType attendanceSession)
    {
        if (!Enum.IsDefined(attendanceSession))
        {
            throw new InvalidOperationException("A valid attendance session is required.");
        }
    }

    private static void ValidateAttendanceEntries(IEnumerable<SubmitAttendanceEntryDto> entries)
    {
        var items = entries?.ToList() ?? [];
        if (items.Count == 0)
        {
            throw new InvalidOperationException("Attendance entries are required.");
        }

        if (items.Any(item => !Enum.IsDefined(item.AttendanceEntry)))
        {
            throw new InvalidOperationException("Attendance entry contains an invalid status.");
        }
    }

    private static void ValidateDateRange(DateTime fromDate, DateTime toDate)
    {
        if (fromDate.Date > toDate.Date)
        {
            throw new InvalidOperationException("From date cannot be greater than to date.");
        }
    }

    private static string NormalizeRole(string role) =>
        (role ?? string.Empty).Trim().Replace(" ", string.Empty).ToLowerInvariant();

    private static string NormalizeRequired(string? value, string fieldName)
    {
        var normalized = NormalizeOptional(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException($"{fieldName} is required.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
