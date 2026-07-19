using SmartContractPlatform.DAL.Enums;

namespace SmartContractPlatform.BLL.DTOs;

public record UserDto(
    Guid Id,
    UserType UserType,
    UserRole Role,
    string? FinCode,
    string? Voen,
    string Name,
    string? Surname,
    string? FatherName,
    string? CompanyName,
    string? RepresentativeName,
    string Email,
    string Phone);

public record UpdateUserDto(
    string? Name,
    string? Surname,
    string? FatherName,
    string? CompanyName,
    string? RepresentativeName,
    string Phone,
    string Email);
