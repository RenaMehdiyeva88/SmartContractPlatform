using SmartContractPlatform.DAL.Enums;

namespace SmartContractPlatform.BLL.DTOs;

public record ContractDto(
    Guid Id,
    Guid TemplateId,
    string? TemplateName,
    Guid CreatedByUserId,
    ContractStatus Status,
    string DataJson,
    DateTime CreatedAt,
    IReadOnlyList<ContractPartyDto> Parties);

public record CreateContractDto(
    Guid TemplateId,
    string DataJson,
    string OtherPartyIdentificationNumber,
    PartyRole CreatorRole,
    PartyRole OtherPartyRole,
    /// <summary>Yalnız qarşı tərəf sistemdə qeydiyyatdan keçməyibsə tələb olunur</summary>
    string? OtherPartyFullName = null,
    /// <summary>Yalnız qarşı tərəf sistemdə qeydiyyatdan keçməyibsə — dəvət üçün</summary>
    string? OtherPartyContactEmail = null,
    /// <summary>Yalnız qarşı tərəf sistemdə qeydiyyatdan keçməyibsə — dəvət üçün</summary>
    string? OtherPartyContactPhone = null);

public record UpdateContractDto(string DataJson);

public record RejectContractDto(string Reason);

public record CreateContractPartyDto(
    string IdentificationNumber,
    string FullNameOrCompany,
    PartyRole Role,
    string? ContactEmail = null,
    string? ContactPhone = null);

public record UpdateContractPartyDto(
    string IdentificationNumber,
    string FullNameOrCompany,
    PartyRole Role,
    string? ContactEmail = null,
    string? ContactPhone = null);

public record ContractPartyDto(
    Guid Id,
    Guid ContractId,
    Guid? UserId,
    string IdentificationNumber,
    string FullNameOrCompany,
    PartyRole Role,
    bool Signed,
    DateTime? SignedAt);