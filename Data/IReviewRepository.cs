using CareerSync.Models;

namespace CareerSync.Data
{
    public interface IReviewRepository : IRepositoryBase<Review>
    {
        Review? GetByGraduateId(int graduateId);
    }
}
