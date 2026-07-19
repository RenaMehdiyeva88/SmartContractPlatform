using SmartContractPlatform.BLL.DTOs;

namespace SmartContractPlatform.BLL.Services.Interfaces;

public interface IContractService
{
    Task<ContractDto> CreateAsync(CreateContractDto dto, Guid currentUserId, string ipAddress, CancellationToken cancellationToken = default);

    Task<ContractDto?> GetByIdAsync(Guid id, Guid currentUserId, string ipAddress, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContractDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContractDto>> GetMyContractsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ContractDto?> GetExistingForTemplateAsync(Guid templateId, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<ContractDto?> UpdateAsync(Guid id, UpdateContractDto dto, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<bool> DeleteDraftAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<bool> RejectAsync(Guid id, RejectContractDto dto, Guid currentUserId, string ipAddress, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContractPartyDto>?> GetPartiesAsync(Guid contractId, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<ContractPartyDto?> GetPartyByIdAsync(Guid partyId, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<ContractPartyDto?> AddPartyAsync(Guid contractId, CreateContractPartyDto dto, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<ContractPartyDto?> UpdatePartyAsync(Guid partyId, UpdateContractPartyDto dto, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<bool> DeletePartyAsync(Guid partyId, Guid currentUserId, CancellationToken cancellationToken = default);
}
