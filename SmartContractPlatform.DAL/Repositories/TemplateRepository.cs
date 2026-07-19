using Microsoft.EntityFrameworkCore;
using SmartContractPlatform.DAL.Data;
using SmartContractPlatform.DAL.Entities;
using SmartContractPlatform.DAL.Repositories.Interfaces;

namespace SmartContractPlatform.DAL.Repositories;

public class TemplateRepository : GenericRepository<Template>, ITemplateRepository
{
    public TemplateRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Template>> GetActiveTemplatesAsync()
        => await _dbSet.AsNoTracking().Where(t => t.IsActive).ToListAsync();
}

