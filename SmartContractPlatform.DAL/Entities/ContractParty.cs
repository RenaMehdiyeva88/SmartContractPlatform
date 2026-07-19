using SmartContractPlatform.DAL.Enums;

namespace SmartContractPlatform.DAL.Entities;

public class ContractParty : BaseEntity
{
    public Guid ContractId { get; set; }
    public Contract? Contract { get; set; }

    /// <summary>Nullable - counterparty may not be registered in the system yet</summary>
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    /// <summary>FIN or VOEN of the party, used to find/invite the counterparty</summary>
    public string IdentificationNumber { get; set; } = string.Empty;

    public string FullNameOrCompany { get; set; } = string.Empty;

    public PartyRole Role { get; set; }

    public bool Signed { get; set; } = false;
    public DateTime? SignedAt { get; set; }

    /// <summary>Yalnız UserId null olanda istifadə olunur — dəvət göndərmək üçün</summary>
    public string? ContactEmail { get; set; }

    /// <summary>Yalnız UserId null olanda istifadə olunur — dəvət göndərmək üçün</summary>
    public string? ContactPhone { get; set; }
}