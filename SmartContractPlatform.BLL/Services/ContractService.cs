using SmartContractPlatform.BLL.DTOs;
using SmartContractPlatform.BLL.Services.Interfaces;
using SmartContractPlatform.DAL.Entities;
using SmartContractPlatform.DAL.Enums;
using SmartContractPlatform.DAL.Repositories.Interfaces;
using System.Text.Json;

namespace SmartContractPlatform.BLL.Services;

public class ContractService(
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    IAuditService auditService) : IContractService
{
    public async Task<ContractDto> CreateAsync(CreateContractDto dto, Guid currentUserId, string ipAddress, CancellationToken cancellationToken = default)
    {
        EnsureValidJson(dto.DataJson, nameof(dto.DataJson));

        var template = await unitOfWork.Templates.GetByIdAsync(dto.TemplateId)
            ?? throw new InvalidOperationException("Template was not found.");
        if (!template.IsActive)
        {
            throw new InvalidOperationException("Template is inactive.");
        }

        var creator = await unitOfWork.Users.GetByIdAsync(currentUserId)
            ?? throw new InvalidOperationException("Current user was not found.");

        var identificationNumber = dto.OtherPartyIdentificationNumber.Trim();
        var otherPartyUser = await unitOfWork.Users.GetByIdentificationNumberAsync(identificationNumber);

        if (otherPartyUser is null)
        {
            EnsureUnregisteredPartyInvite(dto.OtherPartyFullName, dto.OtherPartyContactEmail, dto.OtherPartyContactPhone);
        }

        var contract = new Contract
        {
            TemplateId = dto.TemplateId,
            CreatedByUserId = currentUserId,
            DataJson = dto.DataJson,
            Status = ContractStatus.Pending
        };

        await unitOfWork.Contracts.AddAsync(contract);
        await unitOfWork.SaveChangesAsync();

        await unitOfWork.ContractParties.AddAsync(new ContractParty
        {
            ContractId = contract.Id,
            UserId = creator.Id,
            IdentificationNumber = creator.IdentificationNumber,
            FullNameOrCompany = GetDisplayName(creator),
            Role = dto.CreatorRole
        });

        if (otherPartyUser is not null)
        {
            // Qarşı tərəf artıq qeydiyyatdan keçib
            await unitOfWork.ContractParties.AddAsync(new ContractParty
            {
                ContractId = contract.Id,
                UserId = otherPartyUser.Id,
                IdentificationNumber = otherPartyUser.IdentificationNumber,
                FullNameOrCompany = GetDisplayName(otherPartyUser),
                Role = dto.OtherPartyRole
            });

            await unitOfWork.SaveChangesAsync();
            await emailService.SendContractInvitationAsync(contract, otherPartyUser, cancellationToken);
        }
        else
        {
            // Qarşı tərəf sistemdə tapılmadı — dəvət kimi qeyd yaradılır
            await unitOfWork.ContractParties.AddAsync(new ContractParty
            {
                ContractId = contract.Id,
                UserId = null,
                IdentificationNumber = identificationNumber,
                FullNameOrCompany = dto.OtherPartyFullName,
                Role = dto.OtherPartyRole,
                ContactEmail = dto.OtherPartyContactEmail,
                ContactPhone = dto.OtherPartyContactPhone
            });

            await unitOfWork.SaveChangesAsync();
            await emailService.SendUnregisteredInvitationAsync(contract, dto.OtherPartyContactEmail, dto.OtherPartyContactPhone, cancellationToken);
        }

        await auditService.TrackAsync(contract.Id, currentUserId, AuditActions.ContractCreated, ipAddress, cancellationToken);

        return await MapAsync(contract, cancellationToken);
    }

    public async Task<IReadOnlyList<ContractDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var contracts = await unitOfWork.Contracts.GetAllAsync();
        var result = new List<ContractDto>();

        foreach (var contract in contracts)
        {
            result.Add(await MapAsync(contract, cancellationToken));
        }

        return result;
    }

    public async Task<ContractDto?> GetByIdAsync(Guid id, Guid currentUserId, string ipAddress, CancellationToken cancellationToken = default)
    {
        var contract = await unitOfWork.Contracts.GetWithDetailsAsync(id);
        if (contract is null || !await CanAccessAsync(contract, currentUserId))
        {
            return null;
        }

        await LinkMatchingPartyAsync(contract, currentUserId);
        await auditService.TrackAsync(contract.Id, currentUserId, AuditActions.ContractViewed, ipAddress, cancellationToken);
        return await MapAsync(contract, cancellationToken);
    }

    public async Task<IReadOnlyList<ContractDto>> GetMyContractsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetByIdAsync(userId);
        var contracts = await unitOfWork.Contracts.GetByUserAsync(userId, user?.IdentificationNumber, user?.Email);
        var result = new List<ContractDto>();

        foreach (var contract in contracts)
        {
            await LinkMatchingPartyAsync(contract, userId);
            result.Add(await MapAsync(contract, cancellationToken));
        }

        return result;
    }

    public async Task<ContractDto?> GetExistingForTemplateAsync(Guid templateId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetByIdAsync(currentUserId);
        if (user is null)
        {
            return null;
        }

        var contracts = await unitOfWork.Contracts.GetByUserAsync(currentUserId, user.IdentificationNumber, user.Email);
        var contract = contracts
            .Where(item => item.TemplateId == templateId && item.Status != ContractStatus.Rejected)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefault();

        if (contract is null)
        {
            return null;
        }

        await LinkMatchingPartyAsync(contract, currentUserId);
        return await MapAsync(contract, cancellationToken);
    }

    public async Task<ContractDto?> UpdateAsync(Guid id, UpdateContractDto dto, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var contract = await unitOfWork.Contracts.GetWithDetailsAsync(id);
        if (contract is null || contract.CreatedByUserId != currentUserId || contract.Status != ContractStatus.Pending)
        {
            return null;
        }

        EnsureValidJson(dto.DataJson, nameof(dto.DataJson));
        contract.DataJson = dto.DataJson;
        unitOfWork.Contracts.Update(contract);
        await unitOfWork.SaveChangesAsync();

        return await MapAsync(contract, cancellationToken);
    }

    public async Task<bool> DeleteDraftAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var contract = await unitOfWork.Contracts.GetWithDetailsAsync(id);
        if (contract is null || contract.CreatedByUserId != currentUserId || contract.Status != ContractStatus.Pending)
        {
            return false;
        }

        contract.IsDeleted = true;
        unitOfWork.Contracts.Update(contract);
        await unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectAsync(Guid id, RejectContractDto dto, Guid currentUserId, string ipAddress, CancellationToken cancellationToken = default)
    {
        var contract = await unitOfWork.Contracts.GetWithDetailsAsync(id);
        if (contract is null || !await CanAccessAsync(contract, currentUserId) || contract.Status == ContractStatus.FullySigned)
        {
            return false;
        }

        contract.Status = ContractStatus.Rejected;
        await unitOfWork.SaveChangesAsync();
        await auditService.TrackAsync(id, currentUserId, AuditActions.ContractRejected, ipAddress, cancellationToken);

        foreach (var party in contract.Parties.Where(p => p.UserId.HasValue))
        {
            var recipient = await unitOfWork.Users.GetByIdAsync(party.UserId!.Value);
            if (recipient is null)
            {
                continue;
            }

            await emailService.SendContractRejectedAsync(contract, recipient, dto.Reason, cancellationToken);
        }

        return true;
    }

    public async Task<IReadOnlyList<ContractPartyDto>?> GetPartiesAsync(Guid contractId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var contract = await unitOfWork.Contracts.GetWithDetailsAsync(contractId);
        if (contract is null || !await CanAccessAsync(contract, currentUserId))
        {
            return null;
        }

        return contract.Parties.Select(ToPartyDto).ToList();
    }

    public async Task<ContractPartyDto?> GetPartyByIdAsync(Guid partyId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var party = await unitOfWork.ContractParties.GetByIdAsync(partyId);
        if (party is null)
        {
            return null;
        }

        var contract = await unitOfWork.Contracts.GetWithDetailsAsync(party.ContractId);
        return contract is null || !await CanAccessAsync(contract, currentUserId) ? null : ToPartyDto(party);
    }

    public async Task<ContractPartyDto?> AddPartyAsync(Guid contractId, CreateContractPartyDto dto, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var contract = await unitOfWork.Contracts.GetWithDetailsAsync(contractId);
        if (contract is null || contract.CreatedByUserId != currentUserId || contract.Status != ContractStatus.Pending)
        {
            return null;
        }

        var identificationNumber = dto.IdentificationNumber.Trim();
        if (string.IsNullOrWhiteSpace(identificationNumber) || string.IsNullOrWhiteSpace(dto.FullNameOrCompany))
        {
            throw new ArgumentException("Identification number and full name/company are required.");
        }

        var existingUser = await unitOfWork.Users.GetByIdentificationNumberAsync(identificationNumber);
        var party = new ContractParty
        {
            ContractId = contractId,
            UserId = existingUser?.Id,
            IdentificationNumber = identificationNumber,
            FullNameOrCompany = existingUser is null ? dto.FullNameOrCompany.Trim() : GetDisplayName(existingUser),
            Role = dto.Role,
            ContactEmail = existingUser is null ? dto.ContactEmail : null,
            ContactPhone = existingUser is null ? dto.ContactPhone : null
        };

        await unitOfWork.ContractParties.AddAsync(party);
        await unitOfWork.SaveChangesAsync();
        return ToPartyDto(party);
    }

    public async Task<ContractPartyDto?> UpdatePartyAsync(Guid partyId, UpdateContractPartyDto dto, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var party = await unitOfWork.ContractParties.GetByIdAsync(partyId);
        if (party is null)
        {
            return null;
        }

        var contract = await unitOfWork.Contracts.GetWithDetailsAsync(party.ContractId);
        if (contract is null || contract.CreatedByUserId != currentUserId || contract.Status != ContractStatus.Pending || party.Signed)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(dto.IdentificationNumber) || string.IsNullOrWhiteSpace(dto.FullNameOrCompany))
        {
            throw new ArgumentException("Identification number and full name/company are required.");
        }

        var identificationNumber = dto.IdentificationNumber.Trim();
        var existingUser = await unitOfWork.Users.GetByIdentificationNumberAsync(identificationNumber);
        party.UserId = existingUser?.Id;
        party.IdentificationNumber = identificationNumber;
        party.FullNameOrCompany = existingUser is null ? dto.FullNameOrCompany.Trim() : GetDisplayName(existingUser);
        party.Role = dto.Role;
        party.ContactEmail = existingUser is null ? dto.ContactEmail : null;
        party.ContactPhone = existingUser is null ? dto.ContactPhone : null;

        unitOfWork.ContractParties.Update(party);
        await unitOfWork.SaveChangesAsync();
        return ToPartyDto(party);
    }

    public async Task<bool> DeletePartyAsync(Guid partyId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var party = await unitOfWork.ContractParties.GetByIdAsync(partyId);
        if (party is null)
        {
            return false;
        }

        var contract = await unitOfWork.Contracts.GetWithDetailsAsync(party.ContractId);
        if (contract is null || contract.CreatedByUserId != currentUserId || contract.Status != ContractStatus.Pending || party.Signed)
        {
            return false;
        }

        unitOfWork.ContractParties.Remove(party);
        await unitOfWork.SaveChangesAsync();
        return true;
    }

    private async Task<ContractDto> MapAsync(Contract contract, CancellationToken cancellationToken)
    {
        var detailed = await unitOfWork.Contracts.GetWithDetailsAsync(contract.Id) ?? contract;
        var parties = detailed.Parties.Count > 0
            ? detailed.Parties.ToList()
            : (await unitOfWork.ContractParties.FindAsync(party => party.ContractId == contract.Id)).ToList();

        return new ContractDto(
            detailed.Id,
            detailed.TemplateId,
            detailed.Template?.Name,
            detailed.CreatedByUserId,
            detailed.Status,
            detailed.DataJson,
            detailed.CreatedAt,
            parties.Select(ToPartyDto).ToList());
    }

    private static ContractPartyDto ToPartyDto(ContractParty party)
    {
        return new ContractPartyDto(
            party.Id,
            party.ContractId,
            party.UserId,
            party.IdentificationNumber,
            party.FullNameOrCompany,
            party.Role,
            party.Signed,
            party.SignedAt);
    }

    private async Task<bool> CanAccessAsync(Contract contract, Guid userId)
    {
        if (contract.CreatedByUserId == userId || contract.Parties.Any(party => party.UserId == userId))
        {
            return true;
        }

        var user = await unitOfWork.Users.GetByIdAsync(userId);
        return user is not null && contract.Parties.Any(party => PartyMatchesUser(party, user));
    }

    private static bool PartyMatchesUser(ContractParty party, User user)
        => string.Equals(party.IdentificationNumber?.Trim(), user.IdentificationNumber?.Trim(), StringComparison.OrdinalIgnoreCase) ||
           (!string.IsNullOrWhiteSpace(party.ContactEmail) && string.Equals(party.ContactEmail.Trim(), user.Email.Trim(), StringComparison.OrdinalIgnoreCase));

    private async Task LinkMatchingPartyAsync(Contract contract, Guid userId)
    {
        var user = await unitOfWork.Users.GetByIdAsync(userId);
        if (user is null)
        {
            return;
        }

        var detailed = await unitOfWork.Contracts.GetWithDetailsAsync(contract.Id) ?? contract;
        var party = detailed.Parties.FirstOrDefault(item => item.UserId is null && PartyMatchesUser(item, user));
        if (party is null)
        {
            return;
        }

        party.UserId = userId;
        await unitOfWork.SaveChangesAsync();
    }

    private static string GetDisplayName(User user)
        => user.UserType == UserType.Company
            ? user.CompanyName ?? user.RepresentativeName ?? user.Email
            : string.Join(' ', new[] { user.FirstName, user.LastName }.Where(value => !string.IsNullOrWhiteSpace(value)));

    private static void EnsureUnregisteredPartyInvite(string? fullNameOrCompany, string? contactEmail, string? contactPhone)
    {
        if (string.IsNullOrWhiteSpace(fullNameOrCompany))
        {
            throw new ArgumentException(
                "Qarşı tərəf sistemdə tapılmadı. Bu halda ad/şirkət adı (OtherPartyFullName) tələb olunur.",
                nameof(fullNameOrCompany));
        }

        if (string.IsNullOrWhiteSpace(contactEmail) && string.IsNullOrWhiteSpace(contactPhone))
        {
            throw new ArgumentException(
                "Qarşı tərəf sistemdə tapılmadı. Dəvət göndərmək üçün email və ya telefon tələb olunur.",
                nameof(contactEmail));
        }
    }

    private static void EnsureValidJson(string json, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON data is required.", parameterName);
        }

        JsonDocument.Parse(json);
    }
}