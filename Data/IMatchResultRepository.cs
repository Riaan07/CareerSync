using CareerSync.Models;

namespace CareerSync.Data
{
    public interface IMatchResultRepository : IRepositoryBase<MatchResult>
    {
        IReadOnlyList<MatchResult> GetByCvId(int cvId);
        IReadOnlyList<MatchResult> GetByJobDescriptionId(int jobDescriptionId);
    }
}
