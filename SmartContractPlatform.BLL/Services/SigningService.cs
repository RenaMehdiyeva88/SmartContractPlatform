using SmartContractPlatform.BLL.DTOs;
using SmartContractPlatform.BLL.Services.Interfaces;
using SmartContractPlatform.DAL.Entities;
using SmartContractPlatform.DAL.Enums;
using SmartContractPlatform.DAL.Repositories.Interfaces;

namespace SmartContractPlatform.BLL.Services;

public class SigningService(
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    IAuditService auditService) : ISigningService
{
    public async Task<IReadOnlyList<SignatureDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var signatures = await unitOfWork.Signatures.GetAllAsync();
        return signatures.Select(ToDto).ToList();
    }

    public async Task<SignatureDto?> GetByIdAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var signature = await unitOfWork.Signatures.GetByIdAsync(id);
        if (signature is null || !await CanAccessSignatureAsync(signature, currentUserId))
        {
            return null;
        }

        return ToDto(signature);
    }

    public async Task<IReadOnlyList<SignatureDto>?> GetByContractIdAsync(Guid contractId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var contract = await unitOfWork.Contracts.GetWithDetailsAsync(contractId);
        if (contract is null || !await CanAccessAsync(contract, currentUserId))
        {
            return null;
        }

        var signatures = await unitOfWork.Signatures.FindAsync(signature => signature.ContractId == contractId);
        return signatures.Select(ToDto).ToList();
    }

    public async Task<SignatureDto?> SignAsync(Guid contractId, SignContractDto dto, Guid currentUserId, string ipAddress, CancellationToken cancellationToken = default)
    {
        var contract = await unitOfWork.Contracts.GetWithDetailsAsync(contractId);

        if (contract is null)
        {
            throw new InvalidOperationException("Müqavilə tapılmadı.");
        }

        if (contract.Status is ContractStatus.FullySigned or ContractStatus.Rejected)
        {
            throw new InvalidOperationException("Bu müqavilə artıq yekunlaşıb və imzalana bilməz.");
        }

        var user = await unitOfWork.Users.GetByIdAsync(currentUserId)
            ?? throw new InvalidOperationException("İstifadəçi tapılmadı.");
        var party = contract.Parties.FirstOrDefault(item => item.UserId == currentUserId) ??
                    contract.Parties.FirstOrDefault(item => PartyMatchesUser(item, user));
        if (party is null)
        {
            throw new InvalidOperationException("Siz bu müqavilənin tərəfi deyilsiniz.");
        }

        if (party.Signed)
        {
            throw new InvalidOperationException("Bu tərəf artıq müqaviləni imzalayıb.");
        }

        if (party.UserId is null)
        {
            party.UserId = currentUserId;
        }

        var signature = new Signature
        {
            ContractId = contractId,
            UserId = currentUserId,
            SignatureType = dto.SignatureType,
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        };

        await unitOfWork.Signatures.AddAsync(signature);

        party.Signed = true;
        party.SignedAt = DateTime.UtcNow;

        contract.Status = contract.Parties.All(item => item.Id == party.Id || item.Signed)
            ? ContractStatus.FullySigned
            : ContractStatus.PartiallySigned;

        await unitOfWork.SaveChangesAsync();
        await auditService.TrackAsync(contractId, currentUserId, AuditActions.ContractSigned, ipAddress, cancellationToken);

        foreach (var partyRecipient in contract.Parties.Where(item => item.UserId.HasValue))
        {
            var recipient = await unitOfWork.Users.GetByIdAsync(partyRecipient.UserId!.Value);
            if (recipient is null)
            {
                continue;
            }

            await emailService.SendContractSignedAsync(contract, recipient, cancellationToken);
        }

        return ToDto(signature);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var signature = await unitOfWork.Signatures.GetByIdAsync(id);
        if (signature is null)
        {
            return false;
        }

        var contract = await unitOfWork.Contracts.GetWithDetailsAsync(signature.ContractId);
        if (contract is not null && contract.Status != ContractStatus.Rejected)
        {
            var party = contract.Parties.FirstOrDefault(item => item.UserId == signature.UserId);
            if (party is not null)
            {
                party.Signed = false;
                party.SignedAt = null;
                unitOfWork.ContractParties.Update(party);
            }

            contract.Status = contract.Parties.Any(item => item.UserId != signature.UserId && item.Signed)
                ? ContractStatus.PartiallySigned
                : ContractStatus.Pending;
            unitOfWork.Contracts.Update(contract);
        }

        unitOfWork.Signatures.Remove(signature);
        await unitOfWork.SaveChangesAsync();
        return true;
    }

    private async Task<bool> CanAccessSignatureAsync(Signature signature, Guid currentUserId)
    {
        var contract = await unitOfWork.Contracts.GetWithDetailsAsync(signature.ContractId);
        return contract is not null && await CanAccessAsync(contract, currentUserId);
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

    private static SignatureDto ToDto(Signature signature)
        => new(signature.Id, signature.ContractId, signature.UserId, signature.SignatureType, signature.Timestamp, signature.IpAddress);
}