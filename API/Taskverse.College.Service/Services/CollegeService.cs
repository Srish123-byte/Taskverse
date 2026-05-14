using Microsoft.EntityFrameworkCore;
using Taskverse.API.College.Service.Models;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.College.Service.Services;

public class CollegeService : ICollegeService
{
    private const string CollegeAdminRole = "CollegeAdmin";

    private readonly TaskverseContext _context;

    private sealed record CollegeAdminSummary(Guid CollegeId, string? FullName, string? Email);

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

    public async Task<List<CollegeSearchResultRecord>> SearchColleges(CollegeSearchRequest request)
    {
        var colleges = await _context.Colleges
            .AsNoTracking()
            .OrderBy(college => college.Name)
            .ToListAsync();

        var adminUsers = await _context.Users
            .AsNoTracking()
            .Where(user => user.CollegeId.HasValue && user.Role == CollegeAdminRole)
            .OrderBy(user => user.CreatedAt)
            .Select(user => new CollegeAdminSummary(
                user.CollegeId!.Value,
                user.FullName,
                user.Email))
            .ToListAsync();

        var totalUsersByCollege = await _context.Users
            .AsNoTracking()
            .Where(user => user.CollegeId.HasValue)
            .GroupBy(user => user.CollegeId!.Value)
            .Select(group => new
            {
                CollegeId = group.Key,
                TotalUsers = group.Count()
            })
            .ToDictionaryAsync(item => item.CollegeId, item => item.TotalUsers);

        var adminByCollege = adminUsers
            .GroupBy(user => user.CollegeId)
            .ToDictionary(
                group => group.Key,
                group => group.First());

        var items = colleges
            .Select(college =>
            {
                adminByCollege.TryGetValue(college.CollegeId, out var admin);
                totalUsersByCollege.TryGetValue(college.CollegeId, out var totalUsers);

                return new CollegeSearchResultRecord(
                    college.CollegeId.ToString(),
                    college.Name,
                    college.City,
                    college.State,
                    admin?.FullName,
                    admin?.Email,
                    totalUsers,
                    NormalizeCollegeStatus(college.Status));
            })
            .Where(item => MatchesStatus(item.Status, request.Status))
            .Where(item => MatchesQuery(item, request.Query))
            .ToList();

        return items;
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

    private static bool MatchesStatus(string status, string? requestedStatus)
    {
        if (string.IsNullOrWhiteSpace(requestedStatus) ||
            requestedStatus.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return status.Equals(requestedStatus, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesQuery(CollegeSearchResultRecord item, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var normalizedQuery = query.Trim();
        return Contains(item.Name, normalizedQuery)
            || Contains(item.City, normalizedQuery)
            || Contains(item.State, normalizedQuery)
            || Contains(item.AdminName, normalizedQuery)
            || Contains(item.AdminEmail, normalizedQuery);
    }

    private static bool Contains(string? source, string query) =>
        !string.IsNullOrWhiteSpace(source)
        && source.Contains(query, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeCollegeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return "Pending";
        }

        return status.Trim().ToLowerInvariant() switch
        {
            "active" => "Approved",
            "inactive" => "Suspended",
            _ => status
        };
    }
}
