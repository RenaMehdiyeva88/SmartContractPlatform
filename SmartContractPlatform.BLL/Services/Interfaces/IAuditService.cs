using SmartContractPlatform.BLL.DTOs;

namespace SmartContractPlatform.BLL.Services.Interfaces;

public interface IAuditService
{
    Task<AuditLogDto> TrackAsync(Guid contractId, Guid? userId, string action, string ipAddress, CancellationToken cancellationToken = default);
}
