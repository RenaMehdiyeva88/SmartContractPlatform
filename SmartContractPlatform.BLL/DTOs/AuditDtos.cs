namespace SmartContractPlatform.BLL.DTOs;

public record AuditLogDto(Guid Id, Guid ContractId, Guid? UserId, string Action, string IpAddress, DateTime Timestamp);

public static class AuditActions
{
    public const string ContractViewed = "ContractViewed";
    public const string ContractCreated = "ContractCreated";
    public const string ContractSigned = "ContractSigned";
    public const string ContractRejected = "ContractRejected";
}
