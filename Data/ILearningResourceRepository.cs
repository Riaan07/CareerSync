using CareerSync.Models;

namespace CareerSync.Data
{
    public interface ILearningResourceRepository : IRepositoryBase<LearningResource>
    {
        IReadOnlyList<LearningResource> GetBySkillName(string skillName);
    }
}
