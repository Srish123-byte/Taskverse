using Microsoft.EntityFrameworkCore;
using Taskverse.API.College.Service.Models;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.College.Service.Services;

public class CollegeService : ICollegeService
{
    private readonly TaskverseContext _context;

    public CollegeService(TaskverseContext context)
    {
        _context = context;
    }

    public async Task<List<RegistrationCollegeRecord>> GetApprovedRegistrationColleges()
    {
        return await _context.Colleges
            .AsNoTracking()
            .Where(college => college.Status == CollegeStatuses.Active)
            .OrderBy(college => college.Name)
            .Select(college => new RegistrationCollegeRecord(
                college.CollegeId.ToString(),
                college.Name))
            .ToListAsync();
    }

    public async Task<List<RegistrationClassRecord>> GetRegistrationClasses(Guid collegeId)
    {
        return await _context.Classes
            .AsNoTracking()
            .Where(item => item.CollegeId == collegeId)
            .OrderBy(item => item.Name)
            .ThenBy(item => item.AcademicYear)
            .Select(item => new RegistrationClassRecord(
                item.ClassId.ToString(),
                item.CollegeId.ToString(),
                item.Name,
                item.AcademicYear))
            .ToListAsync();
    }

    public async Task<List<RegistrationBatchRecord>> GetRegistrationBatches(Guid classId)
    {
        return await _context.Batches
            .AsNoTracking()
            .Where(item => item.ClassId == classId)
            .OrderBy(item => item.Name)
            .Select(item => new RegistrationBatchRecord(
                item.BatchId.ToString(),
                item.ClassId.ToString(),
                item.CollegeId.ToString(),
                item.Name))
            .ToListAsync();
    }

    public IReadOnlyList<CollegeRecord> GetColleges()
    {
        return CollegeStore.Colleges;
    }

    public List<CollegeRecord> GetPendingColleges()
    {
        return CollegeStore.Colleges
            .Where(college => college.ApprovalStatus == ApprovalStatuses.Pending)
            .ToList();
    }

    public CollegeRecord? GetCollege(Guid collegeId)
    {
        return CollegeStore.Colleges.FirstOrDefault(item => item.CollegeId == collegeId);
    }

    public CollegeRecord? ApproveCollege(Guid collegeId, CollegeActionRequest request)
    {
        var college = GetCollege(collegeId);
        if (college is null)
        {
            return null;
        }

        var updated = college with
        {
            ApprovalStatus = ApprovalStatuses.Approved,
            Status = CollegeStatuses.Active,
            IsActive = true,
            ApprovedAt = DateTime.UtcNow,
            ApprovedBy = request.PerformedBy,
            Notes = request.Reason
        };

        CollegeStore.Replace(updated);
        return updated;
    }

    public CollegeRecord? RejectCollege(Guid collegeId, CollegeActionRequest request)
    {
        var college = GetCollege(collegeId);
        if (college is null)
        {
            return null;
        }

        var updated = college with
        {
            ApprovalStatus = ApprovalStatuses.Rejected,
            Status = CollegeStatuses.Rejected,
            IsActive = false,
            ApprovedAt = null,
            ApprovedBy = request.PerformedBy,
            Notes = request.Reason
        };

        CollegeStore.Replace(updated);
        return updated;
    }

    public CollegeRecord? DeactivateCollege(Guid collegeId, CollegeActionRequest request)
    {
        var college = GetCollege(collegeId);
        if (college is null)
        {
            return null;
        }

        var updated = college with
        {
            Status = CollegeStatuses.Inactive,
            IsActive = false,
            Notes = request.Reason,
            ApprovedBy = request.PerformedBy
        };

        CollegeStore.Replace(updated);
        return updated;
    }

    public CollegeRecord? ReactivateCollege(Guid collegeId, CollegeActionRequest request)
    {
        var college = GetCollege(collegeId);
        if (college is null)
        {
            return null;
        }

        var updated = college with
        {
            Status = CollegeStatuses.Active,
            IsActive = true,
            Notes = request.Reason,
            ApprovedBy = request.PerformedBy
        };

        CollegeStore.Replace(updated);
        return updated;
    }
}
