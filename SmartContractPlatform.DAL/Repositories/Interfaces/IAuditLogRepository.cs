using SmartContractPlatform.DAL.Entities;

namespace SmartContractPlatform.DAL.Repositories.Interfaces;

/// <summary>
/// AuditLog append-only olmalıdır (bax: sənəd bölmə 4.6 — "Audit trail dəyişdirilə bilməz").
/// Qəsdən Update/Remove metodları YOXDUR.
/// </summary>
public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log);
    Task<IReadOnlyList<AuditLog>> GetByContractAsync(Guid contractId);
    Task<IReadOnlyList<AuditLog>> GetByUserAsync(Guid userId);
}