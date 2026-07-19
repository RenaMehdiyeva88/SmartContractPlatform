namespace SmartContractPlatform.DAL.Repositories.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    ITemplateRepository Templates { get; }
    IContractRepository Contracts { get; }
    IGenericRepository<Entities.ContractParty> ContractParties { get; }
    IGenericRepository<Entities.Signature> Signatures { get; }
    IGenericRepository<Entities.Document> Documents { get; }

    /// <summary>Append-only — Update/Remove yoxdur</summary>
    IAuditLogRepository AuditLogs { get; }

    Task<int> SaveChangesAsync();
}