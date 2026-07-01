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
            .OrderBy(college => college.CollegeName ?? string.Empty)
            .Select(college => new RegistrationCollegeRecord(
                college.CollegeId.ToString(),
                college.CollegeName ?? "Unnamed College"))
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
            .OrderBy(college => college.CollegeName ?? string.Empty)
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
                    college.CollegeName ?? "Unnamed College",
                    college.City,
                    college.State,
                    string.IsNullOrWhiteSpace(college.AdminName) ? admin?.FullName : college.AdminName,
                    admin?.Email,
                    totalUsers,
                    NormalizeCollegeStatus(college.Status));
            })
            .Where(item => MatchesStatus(item.Status, request.Status))
            .Where(item => MatchesQuery(item, request.Query))
            .ToList();

        return items;
    }

    public async Task<List<CollegeRecord>> GetColleges()
    {
        var colleges = await _context.Colleges
            .AsNoTracking()
            .OrderBy(c => c.CollegeName ?? string.Empty)
            .ToListAsync();

        return colleges.Select(ToCollegeRecord).ToList();
    }

    public async Task<List<CollegeRecord>> GetPendingColleges()
    {
        var colleges = await _context.Colleges
            .AsNoTracking()
            .Where(c => c.Status == null || c.Status == "Pending")
            .OrderBy(c => c.CollegeName ?? string.Empty)
            .ToListAsync();

        return colleges.Select(ToCollegeRecord).ToList();
    }

    public async Task<CollegeRecord?> GetCollege(Guid collegeId)
    {
        var college = await _context.Colleges
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CollegeId == collegeId);

        return college is null ? null : ToCollegeRecord(college);
    }

    public async Task<CollegeRecord?> ApproveCollege(Guid collegeId, CollegeActionRequest request)
    {
        var college = await _context.Colleges.FirstOrDefaultAsync(c => c.CollegeId == collegeId);
        if (college is null) return null;

        college.Status = CollegeStatuses.Active;
        college.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return ToCollegeRecord(college);
    }

    public async Task<CollegeRecord?> RejectCollege(Guid collegeId, CollegeActionRequest request)
    {
        var college = await _context.Colleges.FirstOrDefaultAsync(c => c.CollegeId == collegeId);
        if (college is null) return null;

        college.Status = CollegeStatuses.Rejected;
        college.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return ToCollegeRecord(college);
    }

    public async Task<CollegeRecord?> DeactivateCollege(Guid collegeId, CollegeActionRequest request)
    {
        var college = await _context.Colleges.FirstOrDefaultAsync(c => c.CollegeId == collegeId);
        if (college is null) return null;

        college.Status = CollegeStatuses.Inactive;
        college.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return ToCollegeRecord(college);
    }

    public async Task<CollegeRecord?> ReactivateCollege(Guid collegeId, CollegeActionRequest request)
    {
        var college = await _context.Colleges.FirstOrDefaultAsync(c => c.CollegeId == collegeId);
        if (college is null) return null;

        college.Status = CollegeStatuses.Active;
        college.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return ToCollegeRecord(college);
    }

    private static CollegeRecord ToCollegeRecord(Taskverse.Data.DataAccess.College college)
    {
        var normalizedStatus = college.Status?.Trim() ?? string.Empty;
        var isActive = normalizedStatus.Equals(CollegeStatuses.Active, StringComparison.OrdinalIgnoreCase);
        var approvalStatus = normalizedStatus.Equals(CollegeStatuses.Rejected, StringComparison.OrdinalIgnoreCase)
            ? ApprovalStatuses.Rejected
            : isActive || normalizedStatus.Equals(CollegeStatuses.Inactive, StringComparison.OrdinalIgnoreCase)
                ? ApprovalStatuses.Approved
                : ApprovalStatuses.Pending;

        return new CollegeRecord(
            CollegeId: college.CollegeId,
            Name: college.CollegeName ?? "Unnamed College",
            AdminName: college.AdminName,
            City: college.City,
            State: college.State,
            Status: isActive ? CollegeStatuses.Active : (normalizedStatus.Length > 0 ? normalizedStatus : "Pending"),
            ApprovalStatus: approvalStatus,
            IsActive: isActive,
            RequestedAt: college.CreatedAt,
            RequestedBy: null,
            ApprovedAt: null,
            ApprovedBy: null,
            Notes: null);
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
