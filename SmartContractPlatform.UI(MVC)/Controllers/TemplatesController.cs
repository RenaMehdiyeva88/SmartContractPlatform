using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartContractPlatform.BLL.Services.Interfaces;

namespace SmartContractPlatform.MVC.Controllers;

[Authorize]
public class TemplatesController(ITemplateService templateService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var templates = await templateService.GetAllAsync(cancellationToken);
        return View(templates);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var template = await templateService.GetByIdAsync(id, cancellationToken);
        return template is null ? NotFound() : View(template);
    }
}
