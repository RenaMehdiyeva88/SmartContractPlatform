using SmartContractPlatform.DAL.Enums;

namespace SmartContractPlatform.DAL.Entities;

public class Contract : BaseEntity
{
    public Guid TemplateId { get; set; }
    public Template? Template { get; set; }

    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    /// <summary>JSON with the values filled by the creator based on the template schema</summary>
    public string DataJson { get; set; } = string.Empty;

    public ContractStatus Status { get; set; } = ContractStatus.Draft;

    public ICollection<ContractParty> Parties { get; set; } = new List<ContractParty>();
    public ICollection<Signature> Signatures { get; set; } = new List<Signature>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
