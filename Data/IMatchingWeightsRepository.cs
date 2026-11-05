using CareerSync.Models;

namespace CareerSync.Data
{
    public interface IMatchingWeightsRepository : IRepositoryBase<MatchingWeights>
    {
        MatchingWeights GetCurrent();
    }
}
