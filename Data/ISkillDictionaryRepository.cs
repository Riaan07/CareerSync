using CareerSync.Models;

namespace CareerSync.Data
{
    public interface ISkillDictionaryRepository : IRepositoryBase<SkillDictionaryTerm>
    {
        bool TermExists(string term);
    }
}
