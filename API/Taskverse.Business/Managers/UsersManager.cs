using MongoDB.Driver;
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
        FilterDefinition<User> filter = Builders<User>.Filter.Eq(
            u => u.Email, email.ToLowerInvariant());

        return await _context.Users.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<User?> GetById(string userId)
    {
        FilterDefinition<User> filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        return await _context.Users.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<User> Create(User user)
    {
        await _context.Users.InsertOneAsync(user);
        return user;
    }

    public async Task Update(User user)
    {
        FilterDefinition<User> filter = Builders<User>.Filter.Eq(u => u.Id, user.Id);
        await _context.Users.ReplaceOneAsync(filter, user);
    }

    public async Task Delete(string userId)
    {
        FilterDefinition<User> filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        User? user = await _context.Users.Find(filter).FirstOrDefaultAsync();

        if (user is null)
            return;

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.Users.ReplaceOneAsync(filter, user);
    }
}
