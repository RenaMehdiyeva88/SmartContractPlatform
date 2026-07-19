using SmartContractPlatform.BLL.DTOs;
using SmartContractPlatform.BLL.Services.Interfaces;
using SmartContractPlatform.DAL.Entities;
using SmartContractPlatform.DAL.Repositories.Interfaces;
using System.Text.Json;

namespace SmartContractPlatform.BLL.Services;

public class TemplateService(IUnitOfWork unitOfWork) : ITemplateService
{
    public async Task<IReadOnlyList<TemplateDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var templates = await unitOfWork.Templates.GetAllAsync();
        return templates.Select(ToDto).ToList();
    }

    public async Task<TemplateDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await unitOfWork.Templates.GetByIdAsync(id);
        return template is null ? null : ToDto(template);
    }

    public async Task<TemplateDto> CreateAsync(CreateTemplateDto dto, CancellationToken cancellationToken = default)
    {
        EnsureValidSchema(dto.SchemaJson);

        var template = new Template
        {
            Name = dto.Name,
            Category = dto.Category,
            SchemaJson = dto.SchemaJson,
            IsActive = dto.IsActive
        };

        await unitOfWork.Templates.AddAsync(template);
        await unitOfWork.SaveChangesAsync();

        return ToDto(template);
    }

    public async Task<TemplateDto?> UpdateAsync(Guid id, UpdateTemplateDto dto, CancellationToken cancellationToken = default)
    {
        var template = await unitOfWork.Templates.GetByIdAsync(id);

        if (template is null)
        {
            return null;
        }

        EnsureValidSchema(dto.SchemaJson);

        template.Name = dto.Name;
        template.Category = dto.Category;
        template.SchemaJson = dto.SchemaJson;
        template.IsActive = dto.IsActive;

        unitOfWork.Templates.Update(template);
        await unitOfWork.SaveChangesAsync();

        return ToDto(template);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await unitOfWork.Templates.GetByIdAsync(id);
        if (template is null)
        {
            return;
        }

        template.IsDeleted = true;
        unitOfWork.Templates.Update(template);
        await unitOfWork.SaveChangesAsync();
    }

    private static TemplateDto ToDto(Template template)
    {
        using var document = JsonDocument.Parse(template.SchemaJson);
        return new TemplateDto(template.Id, template.Name, template.Category, template.SchemaJson, document.RootElement.Clone(), template.IsActive);
    }

    private static void EnsureValidSchema(string schemaJson)
    {
        if (string.IsNullOrWhiteSpace(schemaJson))
        {
            throw new ArgumentException("Template JSON schema is required.", nameof(schemaJson));
        }

        JsonDocument.Parse(schemaJson);
    }
}
