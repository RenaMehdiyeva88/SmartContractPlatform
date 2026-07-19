using SmartContractPlatform.BLL.DTOs;

namespace SmartContractPlatform.BLL.Services.Interfaces;

public interface IAuthService
{
    Task<RegisterResultDto> RegisterAsync(RegisterRequestDto dto, CancellationToken cancellationToken = default);

    Task<AuthResponseDto?> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default);
}
