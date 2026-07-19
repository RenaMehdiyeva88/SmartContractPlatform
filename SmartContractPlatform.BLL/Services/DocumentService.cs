using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SmartContractPlatform.BLL.DTOs;
using SmartContractPlatform.BLL.Services.Interfaces;
using SmartContractPlatform.DAL.Entities;
using SmartContractPlatform.DAL.Repositories.Interfaces;

namespace SmartContractPlatform.BLL.Services;

public class DocumentService(IUnitOfWork unitOfWork, IPdfService pdfService) : IDocumentService
{
    public async Task<IReadOnlyList<DocumentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var documents = await unitOfWork.Documents.GetAllAsync();
        return documents.Select(ToDto).ToList();
    }

    public async Task<DocumentDto?> GetByIdAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var document = await unitOfWork.Documents.GetByIdAsync(id);
        if (document is null || !await CanAccessDocumentAsync(document, currentUserId))
        {
            return null;
        }

        return ToDto(document);
    }

    public async Task<DocumentDto?> CreateAsync(CreateDocumentDto dto, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.FileUrl) || string.IsNullOrWhiteSpace(dto.Hash))
        {
            throw new ArgumentException("FileUrl and Hash are required.");
        }

        var contract = await unitOfWork.Contracts.GetWithDetailsAsync(dto.ContractId);
        if (contract is null || !await CanAccessAsync(contract, currentUserId))
        {
            return null;
        }

        var document = new Document
        {
            ContractId = dto.ContractId,
            FileUrl = dto.FileUrl,
            Hash = dto.Hash
        };

        await unitOfWork.Documents.AddAsync(document);
        await unitOfWork.SaveChangesAsync();

        return ToDto(document);
    }

    public async Task<DocumentDto?> UpdateAsync(Guid id, UpdateDocumentDto dto, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.FileUrl) || string.IsNullOrWhiteSpace(dto.Hash))
        {
            throw new ArgumentException("FileUrl and Hash are required.");
        }

        var document = await unitOfWork.Documents.GetByIdAsync(id);
        if (document is null || !await CanAccessDocumentAsync(document, currentUserId))
        {
            return null;
        }

        document.FileUrl = dto.FileUrl;
        document.Hash = dto.Hash;
        unitOfWork.Documents.Update(document);
        await unitOfWork.SaveChangesAsync();
        return ToDto(document);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var document = await unitOfWork.Documents.GetByIdAsync(id);
        if (document is null || !await CanAccessDocumentAsync(document, currentUserId))
        {
            return false;
        }

        unitOfWork.Documents.Remove(document);
        await unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<DocumentDto?> GetByContractIdAsync(Guid contractId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var contract = await unitOfWork.Contracts.GetWithDetailsAsync(contractId);
        if (contract is null || !await CanAccessAsync(contract, currentUserId))
        {
            return null;
        }

        var documents = await unitOfWork.Documents.FindAsync(document => document.ContractId == contractId);
        var document = documents.FirstOrDefault();

        return document is null ? null : ToDto(document);
    }

    public async Task<DocumentDto?> GenerateForContractAsync(Guid contractId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var contract = await unitOfWork.Contracts.GetWithDetailsAsync(contractId);
        if (contract is null || !await CanAccessAsync(contract, currentUserId))
        {
            return null;
        }

        var uniqueHash = Convert.ToHexString(SHA256.HashData(Guid.NewGuid().ToByteArray()));
        var content = pdfService.GenerateContractPdf(contract, uniqueHash);
        var directory = Path.Combine(AppContext.BaseDirectory, "documents");
        Directory.CreateDirectory(directory);

        var fileName = $"contract-{contract.Id:N}.pdf";
        var path = Path.Combine(directory, fileName);
        await File.WriteAllBytesAsync(path, content, cancellationToken);

        try
        {
            var existing = await unitOfWork.Documents.FirstOrDefaultAsync(document => document.ContractId == contractId);
            if (existing is null)
            {
                existing = new Document
                {
                    ContractId = contractId,
                    FileUrl = path,
                    Hash = uniqueHash
                };
                await unitOfWork.Documents.AddAsync(existing);
            }
            else
            {
                existing.FileUrl = path;
                existing.Hash = uniqueHash;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await unitOfWork.SaveChangesAsync();
            return ToDto(existing);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new InvalidOperationException("PDF metadata could not be saved because it was changed by another operation. Please try again.", exception);
        }
    }

    public async Task<DocumentFileDto?> DownloadByContractIdAsync(Guid contractId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var contract = await unitOfWork.Contracts.GetWithDetailsAsync(contractId);
        if (contract is null || !await CanAccessAsync(contract, currentUserId))
        {
            return null;
        }

        var document = (await unitOfWork.Documents.FindAsync(item => item.ContractId == contractId)).FirstOrDefault();
        if (document is null || !File.Exists(document.FileUrl))
        {
            return null;
        }

        var content = await File.ReadAllBytesAsync(document.FileUrl, cancellationToken);
        return new DocumentFileDto(Path.GetFileName(document.FileUrl), "application/pdf", content);
    }

    private static DocumentDto ToDto(Document document)
        => new(document.Id, document.ContractId, document.FileUrl, document.Hash, document.CreatedAt);

    private async Task<bool> CanAccessDocumentAsync(Document document, Guid userId)
    {
        var contract = await unitOfWork.Contracts.GetWithDetailsAsync(document.ContractId);
        return contract is not null && await CanAccessAsync(contract, userId);
    }

    private async Task<bool> CanAccessAsync(Contract contract, Guid userId)
    {
        if (contract.CreatedByUserId == userId || contract.Parties.Any(party => party.UserId == userId))
        {
            return true;
        }

        var user = await unitOfWork.Users.GetByIdAsync(userId);
        return user is not null && contract.Parties.Any(party =>
            string.Equals(party.IdentificationNumber?.Trim(), user.IdentificationNumber?.Trim(), StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrWhiteSpace(party.ContactEmail) && string.Equals(party.ContactEmail.Trim(), user.Email.Trim(), StringComparison.OrdinalIgnoreCase)));
    }
}