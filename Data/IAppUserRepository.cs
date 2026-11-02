using CareerSync.Data;
using CareerSync.Models;

namespace CareerSync.Data;

public interface IAppUserRepository : IRepositoryBase<AppUser>
{
    AppUser? FindByEmail(string email);
    bool EmailExists(string email);

    /// <summary>True if the user still exists and is active. Used on every
    /// request to invalidate sign-ins for deleted/deactivated accounts.</summary>
    bool IsActiveUser(int id);
}
