using System.ComponentModel.DataAnnotations;
using SmartContractPlatform.DAL.Enums;

namespace SmartContractPlatform.MVC.Models;

public class TemplateFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Şablon adı tələb olunur")]
    [Display(Name = "Şablon adı")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Kateqoriya")]
    public TemplateCategory Category { get; set; } = TemplateCategory.Rental;

    [Required(ErrorMessage = "JSON schema tələb olunur")]
    [Display(Name = "JSON schema")]
    public string SchemaJson { get; set; } = """
        {
          "type": "object",
          "properties": {
            "contractSubject": { "type": "string" },
            "amount": { "type": "number" },
            "startDate": { "type": "string", "format": "date" }
          },
          "required": ["contractSubject", "amount"]
        }
        """;

    [Display(Name = "Aktivdir")]
    public bool IsActive { get; set; } = true;
}
