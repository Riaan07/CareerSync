using CareerSync.Models;

namespace CareerSync.Data
{
    public class AuditLogRepository : RepositoryBase<AuditLogEntry>, IAuditLogRepository
    {
        public AuditLogRepository(CareerSyncDbContext context) : base(context) { }

        public IReadOnlyList<AuditLogEntry> GetByUserId(int userId) =>
            GetAll().Where(a => a.UserId == userId).OrderByDescending(a => a.Timestamp).ToList();

        public IReadOnlyList<AuditLogEntry> GetRecent(int count) =>
            GetAll().OrderByDescending(a => a.Timestamp).Take(count).ToList();
    }
}
