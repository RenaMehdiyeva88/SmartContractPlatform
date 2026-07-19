using System.ComponentModel.DataAnnotations;
using SmartContractPlatform.DAL.Enums;

namespace SmartContractPlatform.MVC.Models;

public class CreateContractViewModel
{
    [Required]
    [Display(Name = "Şablon")]
    public Guid TemplateId { get; set; }

    [Required(ErrorMessage = "Müqavilə detalları (JSON) tələb olunur")]
    [Display(Name = "Müqavilə detalları (JSON)")]
    public string DataJson { get; set; } = "{}";

    [Required(ErrorMessage = "Qarşı tərəfin FIN/VÖEN-i tələb olunur")]
    [Display(Name = "Qarşı tərəfin FIN/VÖEN-i")]
    public string OtherPartyIdentificationNumber { get; set; } = string.Empty;

    [Display(Name = "Sizin rolunuz")]
    public PartyRole CreatorRole { get; set; } = PartyRole.Owner;

    [Display(Name = "Qarşı tərəfin rolu")]
    public PartyRole OtherPartyRole { get; set; } = PartyRole.Tenant;

    [Display(Name = "Qarşı tərəf sistemdə yoxdursa — adı/şirkəti")]
    public string? OtherPartyFullName { get; set; }

    [Display(Name = "Qarşı tərəf sistemdə yoxdursa — email")]
    public string? OtherPartyContactEmail { get; set; }

    [Display(Name = "Qarşı tərəf sistemdə yoxdursa — telefon")]
    public string? OtherPartyContactPhone { get; set; }
}
