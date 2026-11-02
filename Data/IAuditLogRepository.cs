using CareerSync.Models;

namespace CareerSync.Data
{
    public interface IAuditLogRepository : IRepositoryBase<AuditLogEntry>
    {
        IReadOnlyList<AuditLogEntry> GetByUserId(int userId);
        IReadOnlyList<AuditLogEntry> GetRecent(int count);
    }
}
