using CareerSync.Models;

namespace CareerSync.Data
{
    public interface IMessageRepository : IRepositoryBase<Message>
    {
        IReadOnlyList<Message> GetByGraduateId(int graduateId);
    }
}
