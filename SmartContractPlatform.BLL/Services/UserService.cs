using SmartContractPlatform.BLL.DTOs;
using SmartContractPlatform.BLL.Services.Interfaces;
using SmartContractPlatform.DAL.Enums;
using SmartContractPlatform.DAL.Repositories.Interfaces;

namespace SmartContractPlatform.BLL.Services;

public class UserService(IUnitOfWork unitOfWork) : IUserService
{
    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await unitOfWork.Users.GetAllAsync();
        return users.Select(AuthService.ToDto).ToList();
    }

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetByIdAsync(id);
        return user is null ? null : AuthService.ToDto(user);
    }

    public async Task<UserDto?> GetByIdentificationNumberAsync(string identificationNumber, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetByIdentificationNumberAsync(identificationNumber.Trim());
        return user is null ? null : AuthService.ToDto(user);
    }

    public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            throw new ArgumentException("Email is required.", nameof(dto.Email));
        }

        if (string.IsNullOrWhiteSpace(dto.Phone))
        {
            throw new ArgumentException("Phone is required.", nameof(dto.Phone));
        }

        var user = await unitOfWork.Users.GetByIdAsync(id);
        if (user is null)
        {
            return null;
        }

        if (user.UserType == UserType.Individual)
        {
            user.FirstName = dto.Name;
            user.LastName = dto.Surname;
            user.FatherName = dto.FatherName;
        }
        else
        {
            user.CompanyName = dto.CompanyName ?? dto.Name;
            user.RepresentativeName = dto.RepresentativeName;
        }

        user.Phone = dto.Phone;
        user.Email = dto.Email.Trim();

        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync();

        return AuthService.ToDto(user);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetByIdAsync(id);
        if (user is null)
        {
            return false;
        }

        unitOfWork.Users.Remove(user);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}