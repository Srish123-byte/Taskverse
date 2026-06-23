using Taskverse.Data.DataAccess;

namespace Taskverse.Business.Interface;

public interface IUsersManager
{
    Task<User?> GetByEmail(string email);
    Task<User?> GetById(string userId);
    Task<User> Create(User user);
    Task Update(User user);
    Task Delete(string userId);
}
