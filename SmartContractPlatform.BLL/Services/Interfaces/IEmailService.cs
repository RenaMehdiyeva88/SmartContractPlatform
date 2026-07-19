using SmartContractPlatform.DAL.Entities;

namespace SmartContractPlatform.BLL.Services.Interfaces;

public interface IEmailService
{
    Task SendContractInvitationAsync(Contract contract, User recipient, CancellationToken cancellationToken = default);

    /// <summary>Qarşı tərəf hələ qeydiyyatdan keçməyibsə istifadə olunur</summary>
    Task SendUnregisteredInvitationAsync(Contract contract, string? contactEmail, string? contactPhone, CancellationToken cancellationToken = default);

    Task SendContractSignedAsync(Contract contract, User recipient, CancellationToken cancellationToken = default);

    Task SendContractRejectedAsync(Contract contract, User recipient, string reason, CancellationToken cancellationToken = default);
}