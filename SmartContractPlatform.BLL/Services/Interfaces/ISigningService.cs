using SmartContractPlatform.BLL.DTOs;

namespace SmartContractPlatform.BLL.Services.Interfaces;

public interface ISigningService
{
    Task<IReadOnlyList<SignatureDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<SignatureDto?> GetByIdAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SignatureDto>?> GetByContractIdAsync(Guid contractId, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<SignatureDto?> SignAsync(Guid contractId, SignContractDto dto, Guid currentUserId, string ipAddress, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
