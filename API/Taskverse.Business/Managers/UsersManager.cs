using Microsoft.EntityFrameworkCore;
using Taskverse.Data.Enums;
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
        try
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"An error occurred while retrieving the user by email '{email}'.", ex);
        }
    }

    public async Task<User?> GetById(string userId)
    {
        try
        {
            if (!Guid.TryParse(userId, out Guid id))
                return null;

            return await _context.Users.FindAsync(id);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"An error occurred while retrieving the user by id '{userId}'.", ex);
        }
    }

    public async Task<User> Create(User user)
    {
        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"An error occurred while creating the user '{user.Email}'.", ex);
        }
    }

    public async Task Update(User user)
    {
        try
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"An error occurred while updating the user '{user.Id}'.", ex);
        }
    }

    public async Task Delete(string userId)
    {
        try
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
        catch (Exception ex)
        {
            throw new InvalidOperationException($"An error occurred while deleting the user '{userId}'.", ex);
        }
    }
}
