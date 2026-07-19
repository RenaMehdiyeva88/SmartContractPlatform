using SmartContractPlatform.DAL.Enums;

namespace SmartContractPlatform.BLL.DTOs;

public record SignContractDto(SignatureType SignatureType);

public record SignatureDto(Guid Id, Guid ContractId, Guid UserId, SignatureType SignatureType, DateTime Timestamp, string IpAddress);
