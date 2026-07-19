using SmartContractPlatform.DAL.Enums;

namespace SmartContractPlatform.DAL.Entities;

public class User : BaseEntity
{
    public UserType UserType { get; set; }

    /// <summary>FIN for Individual, VOEN for Company</summary>
    public string IdentificationNumber { get; set; } = string.Empty;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FatherName { get; set; }

    /// <summary>Company name, only for UserType.Company</summary>
    public string? CompanyName { get; set; }

    /// <summary>Representative full name, only for UserType.Company</summary>
    public string? RepresentativeName { get; set; }

    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.User;

    public ICollection<Contract> CreatedContracts { get; set; } = new List<Contract>();
    public ICollection<ContractParty> ContractParties { get; set; } = new List<ContractParty>();
    public ICollection<Signature> Signatures { get; set; } = new List<Signature>();
}
