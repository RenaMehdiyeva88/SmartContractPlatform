namespace SmartContractPlatform.DAL.Entities;

public class AuditLog : BaseEntity
{
    public Guid ContractId { get; set; }
    public Contract? Contract { get; set; }

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public string Action { get; set; } = string.Empty;

    public string IpAddress { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
