using SmartContractPlatform.DAL.Entities;

namespace SmartContractPlatform.DAL.Repositories.Interfaces;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdentificationNumberAsync(string identificationNumber);
    Task<bool> ExistsByEmailAsync(string email);
}

