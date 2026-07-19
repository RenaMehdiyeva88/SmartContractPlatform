using System.Text.Json;
using SmartContractPlatform.DAL.Enums;

namespace SmartContractPlatform.BLL.DTOs;

public record TemplateDto(Guid Id, string Name, TemplateCategory Category, string SchemaJson, JsonElement Schema, bool IsActive);

public record CreateTemplateDto(string Name, TemplateCategory Category, string SchemaJson, bool IsActive = true);

public record UpdateTemplateDto(string Name, TemplateCategory Category, string SchemaJson, bool IsActive);
