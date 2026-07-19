using SmartContractPlatform.BLL.DTOs;
using SmartContractPlatform.BLL.Services.Interfaces;
using SmartContractPlatform.DAL.Entities;
using SmartContractPlatform.DAL.Repositories.Interfaces;

namespace SmartContractPlatform.BLL.Services;

public class AuditService(IUnitOfWork unitOfWork) : IAuditService
{
    public async Task<AuditLogDto> TrackAsync(Guid contractId, Guid? userId, string action, string ipAddress, CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog
        {
            ContractId = contractId,
            UserId = userId,
            Action = action,
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        };

        await unitOfWork.AuditLogs.AddAsync(auditLog);
        await unitOfWork.SaveChangesAsync();

        return new AuditLogDto(auditLog.Id, auditLog.ContractId, auditLog.UserId, auditLog.Action, auditLog.IpAddress, auditLog.Timestamp);
    }
}
