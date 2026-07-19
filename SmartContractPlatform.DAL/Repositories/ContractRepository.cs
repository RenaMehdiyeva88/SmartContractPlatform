using Microsoft.EntityFrameworkCore;
using SmartContractPlatform.DAL.Data;
using SmartContractPlatform.DAL.Entities;
using SmartContractPlatform.DAL.Repositories.Interfaces;

namespace SmartContractPlatform.DAL.Repositories;

public class ContractRepository : GenericRepository<Contract>, IContractRepository
{
    public ContractRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Contract?> GetWithDetailsAsync(Guid id)
        => await _dbSet
            .Include(c => c.Template)
            .Include(c => c.Parties)
            .Include(c => c.Signatures)
            .Include(c => c.Documents)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<IReadOnlyList<Contract>> GetByUserAsync(Guid userId, string? identificationNumber = null, string? email = null)
        => await _dbSet
            .Include(c => c.Template)
            .Include(c => c.Parties)
            .Where(c => c.CreatedByUserId == userId ||
                        c.Parties.Any(p => p.UserId == userId ||
                                           (!string.IsNullOrWhiteSpace(identificationNumber) && p.IdentificationNumber == identificationNumber) ||
                                           (!string.IsNullOrWhiteSpace(email) && p.ContactEmail == email)))
            .AsNoTracking()
            .ToListAsync();
}

