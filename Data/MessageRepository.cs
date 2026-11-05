using CareerSync.Models;

namespace CareerSync.Data
{
    public class MessageRepository : RepositoryBase<Message>, IMessageRepository
    {
        public MessageRepository(CareerSyncDbContext context) : base(context) { }
        public IReadOnlyList<Message> GetByGraduateId(int graduateId) =>
        GetAll().Where(m => m.RecipientGraduateId == graduateId).OrderBy(m => m.SentAt).ToList();
    }
}
