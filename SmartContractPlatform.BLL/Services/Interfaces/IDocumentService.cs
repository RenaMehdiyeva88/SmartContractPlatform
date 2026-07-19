using SmartContractPlatform.BLL.DTOs;

namespace SmartContractPlatform.BLL.Services.Interfaces;

public interface IDocumentService
{
    Task<IReadOnlyList<DocumentDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<DocumentDto?> GetByIdAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<DocumentDto?> CreateAsync(CreateDocumentDto dto, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<DocumentDto?> UpdateAsync(Guid id, UpdateDocumentDto dto, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<DocumentDto?> GetByContractIdAsync(Guid contractId, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<DocumentDto?> GenerateForContractAsync(Guid contractId, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<DocumentFileDto?> DownloadByContractIdAsync(Guid contractId, Guid currentUserId, CancellationToken cancellationToken = default);
}
