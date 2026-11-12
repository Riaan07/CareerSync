using CareerSync.Data;
using CareerSync.Models;

namespace CareerSync.Data;

public interface IAppUserRepository : IRepositoryBase<AppUser>
{
    AppUser? FindByEmail(string email);
    bool EmailExists(string email);

    bool IsActiveUser(int id);
}
