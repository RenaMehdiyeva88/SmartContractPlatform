using System.ComponentModel.DataAnnotations;
using SmartContractPlatform.DAL.Enums;

namespace SmartContractPlatform.MVC.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email tələb olunur")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifrə tələb olunur")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public class RegisterViewModel
{
    [Required]
    public UserType UserType { get; set; } = UserType.Individual;

    [Display(Name = "FIN kod")]
    public string? FinCode { get; set; }

    [Display(Name = "VÖEN")]
    public string? Voen { get; set; }

    [Required(ErrorMessage = "Ad tələb olunur")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Soyad")]
    public string? Surname { get; set; }

    [Display(Name = "Ata adı")]
    public string? FatherName { get; set; }

    [Display(Name = "Şirkət adı")]
    public string? CompanyName { get; set; }

    [Display(Name = "Nümayəndə adı")]
    public string? RepresentativeName { get; set; }

    [Required(ErrorMessage = "Email tələb olunur")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefon tələb olunur")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifrə tələb olunur")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
