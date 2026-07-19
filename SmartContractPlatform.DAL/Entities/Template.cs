using SmartContractPlatform.DAL.Enums;

namespace SmartContractPlatform.DAL.Entities;

public class Template : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public TemplateCategory Category { get; set; }

    /// <summary>JSON schema describing the dynamic fields of this template</summary>
    public string SchemaJson { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}
