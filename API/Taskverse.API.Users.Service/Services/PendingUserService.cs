using Microsoft.EntityFrameworkCore;
using Taskverse.API.Users.Service.DTOs;
using Taskverse.API.Users.Service.Mappings;
using Taskverse.Business.Enums;
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
}
