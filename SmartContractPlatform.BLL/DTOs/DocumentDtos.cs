namespace SmartContractPlatform.BLL.DTOs;

public record DocumentDto(Guid Id, Guid ContractId, string FileUrl, string Hash, DateTime CreatedAt);

public record CreateDocumentDto(Guid ContractId, string FileUrl, string Hash);

public record UpdateDocumentDto(string FileUrl, string Hash);

public record DocumentFileDto(string FileName, string ContentType, byte[] Content);
