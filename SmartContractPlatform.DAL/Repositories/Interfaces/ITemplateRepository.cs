using SmartContractPlatform.DAL.Entities;

namespace SmartContractPlatform.DAL.Repositories.Interfaces;

public interface ITemplateRepository : IGenericRepository<Template>
{
    Task<IReadOnlyList<Template>> GetActiveTemplatesAsync();
}

