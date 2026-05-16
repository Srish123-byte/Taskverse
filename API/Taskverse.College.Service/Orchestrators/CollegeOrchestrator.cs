using Microsoft.EntityFrameworkCore;
using Taskverse.Business.Enums;
using Taskverse.API.College.Service.DTOs;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.College.Service.Orchestrators;

public class CollegeOrchestrator : ICollegeOrchestrator
{
    private readonly TaskverseContext _context;

    public CollegeOrchestrator(TaskverseContext context)
    {
        _context = context;
    }

    public Task<List<PendingUserDto>> GetPendingUsersByCollege(Guid collegeId) =>
        _context.Users
            .AsNoTracking()
            .Where(user =>
                user.Status == UserStatus.PENDING_APPROVAL &&
                user.CollegeId == collegeId &&
                NormalizeRole(user.Role) != "collegeadmin" &&
                NormalizeRole(user.Role) != "superadmin")
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

    public async Task<CollegeBatchSummaryDto> CreateBatch(Guid collegeId, Guid classId, CreateCollegeBatchDto dto)
    {
        var name = NormalizeRequired(dto.Name, "Batch name");
        var capacity = dto.Capacity.GetValueOrDefault();
        if (capacity < 0)
        {
            throw new InvalidOperationException("Batch capacity cannot be negative.");
        }

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
            Capacity = capacity,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        _context.Batches.Add(entity);
        await _context.SaveChangesAsync();

        return new CollegeBatchSummaryDto
        {
            BatchId = entity.BatchId.ToString(),
            ClassId = entity.ClassId.ToString(),
            CollegeId = entity.CollegeId.ToString(),
            Name = entity.Name,
            Capacity = entity.Capacity ?? 0,
            StudentCount = 0,
            CreatedAt = entity.CreatedAt
        };
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

        if (!user.BatchId.HasValue)
        {
            throw new InvalidOperationException($"Student user '{user.Id}' cannot be approved without a batch.");
        }

        var existingStudent = await _context.Students
            .FirstOrDefaultAsync(student => student.UserId == user.Id);

        if (existingStudent is not null)
        {
            existingStudent.ClassId = user.ClassId;
            existingStudent.Status = UserStatus.APPROVED;
            existingStudent.StatusId = (int)UserStatus.APPROVED;
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
            BatchId = user.BatchId.Value,
            FullName = user.FullName,
            Email = user.Email,
            Status = UserStatus.APPROVED,
            StatusId = (int)UserStatus.APPROVED,
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

        var existingTrainer = await _context.Trainers
            .FirstOrDefaultAsync(trainer => trainer.UserId == user.Id);

        if (existingTrainer is not null)
        {
            existingTrainer.Status = UserStatus.APPROVED;
            existingTrainer.StatusId = (int)UserStatus.APPROVED;
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
            Status = UserStatus.APPROVED,
            StatusId = (int)UserStatus.APPROVED,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            ApprovedBy = approvedByUserId
        });
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
