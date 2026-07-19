using Microsoft.EntityFrameworkCore;
using SmartContractPlatform.DAL.Data;
using SmartContractPlatform.DAL.Entities;
using SmartContractPlatform.DAL.Repositories.Interfaces;

namespace SmartContractPlatform.DAL.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<AuditLog> _dbSet;

    public AuditLogRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<AuditLog>();
    }

    public async Task AddAsync(AuditLog log)
    {
        await _dbSet.AddAsync(log);
        await _context.SaveChangesAsync(); // dərhal yazılır, batch-də itməsin
    }

    public async Task<IReadOnlyList<AuditLog>> GetByContractAsync(Guid contractId)
        => await _dbSet.AsNoTracking()
            .Where(a => a.ContractId == contractId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();

    public async Task<IReadOnlyList<AuditLog>> GetByUserAsync(Guid userId)
        => await _dbSet.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
}