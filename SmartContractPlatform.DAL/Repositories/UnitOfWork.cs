using SmartContractPlatform.DAL.Data;
using SmartContractPlatform.DAL.Entities;
using SmartContractPlatform.DAL.Repositories.Interfaces;

namespace SmartContractPlatform.DAL.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    private IUserRepository? _users;
    private ITemplateRepository? _templates;
    private IContractRepository? _contracts;
    private IGenericRepository<ContractParty>? _contractParties;
    private IGenericRepository<Signature>? _signatures;
    private IGenericRepository<Document>? _documents;
    private IAuditLogRepository? _auditLogs;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public ITemplateRepository Templates => _templates ??= new TemplateRepository(_context);
    public IContractRepository Contracts => _contracts ??= new ContractRepository(_context);
    public IGenericRepository<ContractParty> ContractParties => _contractParties ??= new GenericRepository<ContractParty>(_context);
    public IGenericRepository<Signature> Signatures => _signatures ??= new GenericRepository<Signature>(_context);
    public IGenericRepository<Document> Documents => _documents ??= new GenericRepository<Document>(_context);
    public IAuditLogRepository AuditLogs => _auditLogs ??= new AuditLogRepository(_context);

    public async Task<int> SaveChangesAsync()
        => await _context.SaveChangesAsync();

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}