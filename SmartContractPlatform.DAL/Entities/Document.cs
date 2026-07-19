namespace SmartContractPlatform.DAL.Entities;

public class Document : BaseEntity
{
    public Guid ContractId { get; set; }
    public Contract? Contract { get; set; }

    /// <summary>URL/path of the generated PDF stored in MinIO</summary>
    public string FileUrl { get; set; } = string.Empty;

    /// <summary>Unique hash of the generated PDF for integrity verification</summary>
    public string Hash { get; set; } = string.Empty;
}
