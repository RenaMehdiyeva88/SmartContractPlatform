using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SmartContractPlatform.BLL.DTOs;
using SmartContractPlatform.BLL.Services.Interfaces;
using SmartContractPlatform.DAL.Entities;
using SmartContractPlatform.DAL.Enums;
using SmartContractPlatform.DAL.Repositories.Interfaces;

namespace SmartContractPlatform.BLL.Services;

public class AuthService(IUnitOfWork unitOfWork, IConfiguration configuration, ILogger<AuthService> logger) : IAuthService
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    public async Task<RegisterResultDto> RegisterAsync(RegisterRequestDto dto, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateRegisterRequest(dto).ToList();
        if (validationErrors.Count > 0)
        {
            logger.LogWarning("Registration validation failed for email {Email}: {Errors}", dto.Email, string.Join("; ", validationErrors));
            return RegisterResultDto.ValidationFailure(validationErrors.ToArray());
        }

        var identificationNumber = GetIdentificationNumber(dto);

        var existingByEmail = await unitOfWork.Users.GetByEmailAsync(dto.Email.Trim());
        if (existingByEmail is not null)
        {
            logger.LogWarning("Registration conflict: email {Email} already exists for user {UserId}.", dto.Email, existingByEmail.Id);
            return RegisterResultDto.Conflict($"Email '{dto.Email}' already exists.");
        }

        var existingByIdentification = await unitOfWork.Users.GetByIdentificationNumberAsync(identificationNumber);
        if (existingByIdentification is not null)
        {
            var identifierType = dto.UserType == UserType.Individual ? "FIN code" : "VÖEN";
            logger.LogWarning(
                "Registration conflict: {IdentifierType} {IdentificationNumber} already exists for user {UserId}.",
                identifierType,
                identificationNumber,
                existingByIdentification.Id);
            return RegisterResultDto.Conflict($"{identifierType} '{identificationNumber}' already exists.");
        }

        var user = new User
        {
            UserType = dto.UserType,
            IdentificationNumber = identificationNumber,
            FirstName = dto.UserType == UserType.Individual ? dto.Name : null,
            LastName = dto.UserType == UserType.Individual ? dto.Surname : null,
            FatherName = dto.FatherName,
            CompanyName = dto.UserType == UserType.Company ? dto.CompanyName ?? dto.Name : null,
            RepresentativeName = dto.RepresentativeName,
            Email = dto.Email.Trim(),
            Phone = dto.Phone,
            PasswordHash = HashPassword(dto.Password),
            Role = UserRole.User
        };

        try
        {
            await unitOfWork.Users.AddAsync(user);
            logger.LogInformation("Registration phase: saving new user for email {Email}.", dto.Email);
            var savedRows = await unitOfWork.SaveChangesAsync();
            logger.LogInformation("Registration phase: user saved for email {Email}. UserId: {UserId}. SavedRows: {SavedRows}.", dto.Email, user.Id, savedRows);

            if (savedRows <= 0)
            {
                logger.LogError("Registration failed for email {Email}: SaveChangesAsync returned {SavedRows}.", dto.Email, savedRows);
                return RegisterResultDto.ValidationFailure("Qeydiyyat zamanı xəta baş verdi. Bir az sonra yenidən cəhd edin.");
            }
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(
                exception,
                "Registration DbUpdateException while saving user with email {Email}. Exception: {ExceptionMessage}. Inner exception: {InnerExceptionMessage}. Stack trace: {StackTrace}",
                dto.Email,
                exception.Message,
                exception.InnerException?.Message,
                exception.StackTrace);

            return RegisterResultDto.ValidationFailure(
                exception.Message,
                exception.InnerException?.Message,
                exception.StackTrace);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Registration failed while saving user with email {Email}. Exception: {ExceptionMessage}. Inner exception: {InnerExceptionMessage}. Stack trace: {StackTrace}",
                dto.Email,
                exception.Message,
                exception.InnerException?.Message,
                exception.StackTrace);

            return RegisterResultDto.ValidationFailure(
                exception.Message,
                exception.InnerException?.Message,
                exception.StackTrace);
        }

        try
        {
            // Gözləyən dəvətləri (ContractParty.UserId == null) yeni istifadəçiyə bağla
            logger.LogInformation("Registration phase: checking pending contract invitations for user {UserId} and identification number {IdentificationNumber}.", user.Id, identificationNumber);
            var pendingInvites = await unitOfWork.ContractParties.FindAsync(
                party => party.UserId == null && party.IdentificationNumber == identificationNumber);
            logger.LogInformation("Registration phase: found {Count} pending contract invitation(s) for user {UserId}.", pendingInvites.Count, user.Id);

            if (pendingInvites.Count > 0)
            {
                foreach (var invite in pendingInvites)
                {
                    invite.UserId = user.Id;
                    unitOfWork.ContractParties.Update(invite);
                }

                await unitOfWork.SaveChangesAsync();
                logger.LogInformation("Linked {Count} pending contract invitation(s) to new user {UserId}.", pendingInvites.Count, user.Id);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Registration succeeded for user {UserId}, but linking pending contract invitations failed. Exception: {ExceptionMessage}. Inner exception: {InnerExceptionMessage}. Stack trace: {StackTrace}",
                user.Id,
                exception.Message,
                exception.InnerException?.Message,
                exception.StackTrace);
        }

        try
        {
            logger.LogInformation("Registration phase: creating JWT response for user {UserId}.", user.Id);
            var expiresAt = DateTime.UtcNow.AddHours(GetJwtLifetimeHours());
            var response = new AuthResponseDto(CreateToken(user, expiresAt), expiresAt, ToDto(user));
            logger.LogInformation("Registration phase: JWT response created for user {UserId}.", user.Id);
            return RegisterResultDto.Success(response);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Registration succeeded for user {UserId}, but creating JWT response failed. Exception: {ExceptionMessage}. Inner exception: {InnerExceptionMessage}. Stack trace: {StackTrace}",
                user.Id,
                exception.Message,
                exception.InnerException?.Message,
                exception.StackTrace);

            return RegisterResultDto.ValidationFailure(
                exception.Message,
                exception.InnerException?.Message,
                exception.StackTrace);
        }
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetByEmailAsync(dto.Email);

        if (user is null || !VerifyPassword(dto.Password, user.PasswordHash))
        {
            return null;
        }

        var expiresAt = DateTime.UtcNow.AddHours(GetJwtLifetimeHours());
        return new AuthResponseDto(CreateToken(user, expiresAt), expiresAt, ToDto(user));
    }

    internal static UserDto ToDto(User user)
    {
        return new UserDto(
            user.Id,
            user.UserType,
            user.Role,
            user.UserType == UserType.Individual ? user.IdentificationNumber : null,
            user.UserType == UserType.Company ? user.IdentificationNumber : null,
            user.UserType == UserType.Individual ? user.FirstName ?? string.Empty : user.CompanyName ?? string.Empty,
            user.LastName,
            user.FatherName,
            user.CompanyName,
            user.RepresentativeName,
            user.Email,
            user.Phone);
    }

    private static string GetIdentificationNumber(RegisterRequestDto dto)
    {
        return dto.UserType == UserType.Individual
            ? dto.FinCode?.Trim() ?? string.Empty
            : dto.Voen?.Trim() ?? string.Empty;
    }

    private static IEnumerable<string> ValidateRegisterRequest(RegisterRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            yield return "Email is required.";
        }

        if (string.IsNullOrWhiteSpace(dto.Phone))
        {
            yield return "Phone is required.";
        }

        foreach (var passwordError in ValidatePassword(dto.Password))
        {
            yield return passwordError;
        }

        if (dto.UserType == UserType.Individual)
        {
            if (string.IsNullOrWhiteSpace(dto.FinCode))
            {
                yield return "FIN code is required for individual users.";
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                yield return "Name is required for individual users.";
            }

            if (string.IsNullOrWhiteSpace(dto.Surname))
            {
                yield return "Surname is required for individual users.";
            }
        }
        else if (dto.UserType == UserType.Company)
        {
            if (string.IsNullOrWhiteSpace(dto.Voen))
            {
                yield return "VÖEN is required for company users.";
            }

            if (string.IsNullOrWhiteSpace(dto.CompanyName) && string.IsNullOrWhiteSpace(dto.Name))
            {
                yield return "Company name is required for company users.";
            }

            if (string.IsNullOrWhiteSpace(dto.RepresentativeName))
            {
                yield return "Representative name is required for company users.";
            }
        }
        else
        {
            yield return "Invalid user type.";
        }
    }

    private static IEnumerable<string> ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            yield return "Password is required.";
            yield break;
        }

        if (password.Length < 8)
        {
            yield return "Password must be at least 8 characters long.";
        }

        if (!password.Any(char.IsUpper))
        {
            yield return "Password must contain at least one uppercase letter.";
        }

        if (!password.Any(char.IsLower))
        {
            yield return "Password must contain at least one lowercase letter.";
        }

        if (!password.Any(char.IsDigit))
        {
            yield return "Password must contain at least one digit.";
        }
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"PBKDF2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('$');
        if (parts.Length == 4 && parts[0] == "PBKDF2" && int.TryParse(parts[1], out var iterations))
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }

        return false;
    }

    private string CreateToken(User user, DateTime expiresAt)
    {
        var secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret appsettings-də konfiqurasiya olunmayıb.");
        var issuer = configuration["Jwt:Issuer"] ?? "SmartContractPlatform";
        var audience = configuration["Jwt:Audience"] ?? "SmartContractPlatform";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("role", user.Role.ToString()),
            new Claim("roles", user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private double GetJwtLifetimeHours()
        => double.TryParse(configuration["Jwt:LifetimeHours"], out var value) && value > 0 ? value : 2;
}