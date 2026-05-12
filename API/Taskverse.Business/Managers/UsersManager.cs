using Microsoft.EntityFrameworkCore;
using Taskverse.Business.Enums;
using Taskverse.Business.Interface;
using Taskverse.Data;
using Taskverse.Data.DataAccess;

namespace Taskverse.Business.Managers;

public class UsersManager : IUsersManager
{
    private readonly TaskverseContext _context;

    public UsersManager(TaskverseContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<User?> GetByEmail(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
    }

    public async Task<User?> GetById(string userId)
    {
        if (!Guid.TryParse(userId, out Guid id))
            return null;

        return await _context.Users.FindAsync(id);
    }

    public async Task<User> Create(User user)
    {
        _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

    public async Task Update(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task Delete(string userId)
    {
        if (!Guid.TryParse(userId, out Guid id))
            return;

        User? user = await _context.Users.FindAsync(id);
        if (user is null)
            return;

        user.Status = UserStatus.REJECTED;
        user.ModifiedAt = DateTime.UtcNow;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
}
