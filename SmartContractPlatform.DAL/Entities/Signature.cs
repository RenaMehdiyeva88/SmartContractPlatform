using SmartContractPlatform.DAL.Enums;

namespace SmartContractPlatform.DAL.Entities;

public class Signature : BaseEntity
{
    public Guid ContractId { get; set; }
    public Contract? Contract { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public SignatureType SignatureType { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string IpAddress { get; set; } = string.Empty;
}

