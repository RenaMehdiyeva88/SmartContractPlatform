using SmartContractPlatform.DAL.Entities;

namespace SmartContractPlatform.DAL.Repositories.Interfaces;

public interface IContractRepository : IGenericRepository<Contract>
{
    Task<Contract?> GetWithDetailsAsync(Guid id);
    Task<IReadOnlyList<Contract>> GetByUserAsync(Guid userId, string? identificationNumber = null, string? email = null);
}
