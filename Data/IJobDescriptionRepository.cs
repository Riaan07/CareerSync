using CareerSync.Models;

namespace CareerSync.Data
{
    public interface IJobDescriptionRepository : IRepositoryBase<JobDescription>
    {
        IReadOnlyList<JobDescription> GetByAppUserId(int appUserId);
    }
}
