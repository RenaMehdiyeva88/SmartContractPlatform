using SmartContractPlatform.BLL.DTOs;

namespace SmartContractPlatform.BLL.Services.Interfaces;

public interface ITemplateService
{
    Task<IReadOnlyList<TemplateDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TemplateDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TemplateDto> CreateAsync(CreateTemplateDto dto, CancellationToken cancellationToken = default);

    Task<TemplateDto?> UpdateAsync(Guid id, UpdateTemplateDto dto, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
