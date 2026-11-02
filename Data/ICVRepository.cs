using CareerSync.Models;

namespace CareerSync.Data
{
    public interface ICVRepository : IRepositoryBase<CV>
    {
        IReadOnlyList<CV> GetByAppUserId(int appUserId);
    }
}
