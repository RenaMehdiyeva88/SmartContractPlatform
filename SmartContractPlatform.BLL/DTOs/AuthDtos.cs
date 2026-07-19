using SmartContractPlatform.DAL.Enums;

namespace SmartContractPlatform.BLL.DTOs;

public record RegisterRequestDto(
    UserType UserType,
    string? FinCode,
    string? Voen,
    string Name,
    string? Surname,
    string? FatherName,
    string? CompanyName,
    string? RepresentativeName,
    string Email,
    string Phone,
    string Password);

public record LoginRequestDto(string Email, string Password);

public record AuthResponseDto(string AccessToken, DateTime ExpiresAt, UserDto User);

public record RegisterResultDto(bool Succeeded, AuthResponseDto? Response, IReadOnlyList<string> Errors, bool IsConflict)
{
    public static RegisterResultDto Success(AuthResponseDto response) => new(true, response, Array.Empty<string>(), false);

    public static RegisterResultDto ValidationFailure(params string[] errors) => new(false, null, errors, false);

    public static RegisterResultDto Conflict(params string[] errors) => new(false, null, errors, true);
}
