using SmartContractPlatform.BLL.DTOs;

namespace SmartContractPlatform.BLL.Services.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UserDto?> GetByIdentificationNumberAsync(string identificationNumber, CancellationToken cancellationToken = default);

    Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
