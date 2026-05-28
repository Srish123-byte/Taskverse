using Microsoft.EntityFrameworkCore;
using Taskverse.API.Users.Service.DTOs;
using Taskverse.API.Users.Service.Mappings;
using Taskverse.API.Users.Service.Models;
using Taskverse.Data.Enums;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.Users.Service.Services;

public class PendingUserService : IPendingUserService
{
    private readonly TaskverseContext _context;

    public PendingUserService(TaskverseContext context)
    {
        _context = context;
    }

    public async Task<List<PendingUserDto>> GetPendingUsers()
    {
        try
        {
            var pendingUsers = await (
                from user in _context.Users.AsNoTracking()
                where user.Status == UserStatus.PENDING_APPROVAL
                join college in _context.Colleges.AsNoTracking() on user.CollegeId equals college.CollegeId into collegeGroup
                from college in collegeGroup.DefaultIfEmpty()
                join classItem in _context.Classes.AsNoTracking() on user.ClassId equals classItem.ClassId into classGroup
                from classItem in classGroup.DefaultIfEmpty()
                orderby user.CreatedAt
                select new PendingUserProjection(
                    user.Id.ToString(),
                    user.FullName,
                    user.Email,
                    user.Role,
                    user.Status.ToString(),
                    user.CreatedAt,
                    string.IsNullOrWhiteSpace(user.CollegeName) ? (college != null ? college.CollegeName : null) : user.CollegeName))
                .ToListAsync();

            return pendingUsers
                .Select(item => item.ToPendingUserDto())
                .ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while retrieving pending users from the database.", ex);
        }
    }

    public async Task<PagedPendingUsersDto> SearchUsers(UserSearchRequestModel request)
    {
        try
        {
            var userQuery = _context.Users.AsNoTracking();

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(request.Status) &&
                !request.Status.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                if (Enum.TryParse<UserStatus>(request.Status, ignoreCase: true, out var parsedStatus))
                {
                    userQuery = userQuery.Where(u => u.Status == parsedStatus);
                }
            }

            // Apply role filter
            if (!string.IsNullOrWhiteSpace(request.Role) &&
                !request.Role.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                var role = request.Role;
                userQuery = userQuery.Where(u => u.Role == role);
            }

            // Apply name/email search
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                userQuery = userQuery.Where(u =>
                    u.FullName.ToLower().Contains(term) ||
                    u.Email.ToLower().Contains(term));
            }

            var totalCount = await userQuery.CountAsync();

            var projectedQuery =
                from user in userQuery
                join college in _context.Colleges.AsNoTracking() on user.CollegeId equals college.CollegeId into collegeGroup
                from college in collegeGroup.DefaultIfEmpty()
                orderby user.CreatedAt descending
                select new PendingUserProjection(
                    user.Id.ToString(),
                    user.FullName,
                    user.Email,
                    user.Role,
                    user.Status.ToString(),
                    user.CreatedAt,
                    string.IsNullOrWhiteSpace(user.CollegeName)
                        ? (college != null ? college.CollegeName : null)
                        : user.CollegeName);

            var pageSize   = request.PageSize   > 0 ? request.PageSize   : 10;
            var pageNumber = request.PageNumber  > 0 ? request.PageNumber : 1;

            var items = await projectedQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = items.Select(item => item.ToPendingUserDto()).ToList();

            return new PagedPendingUsersDto(dtos, totalCount, pageNumber, pageSize);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while searching users from the database.", ex);
        }
    }
}
