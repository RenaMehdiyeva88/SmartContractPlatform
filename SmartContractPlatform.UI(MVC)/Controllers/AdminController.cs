using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartContractPlatform.BLL.DTOs;
using SmartContractPlatform.BLL.Services.Interfaces;
using SmartContractPlatform.MVC.Models;

namespace SmartContractPlatform.MVC.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(ITemplateService templateService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var templates = await templateService.GetAllAsync(cancellationToken);
        return View(templates);
    }

    [HttpGet]
    public IActionResult Create()
        => View(new TemplateFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TemplateFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await templateService.CreateAsync(
                new CreateTemplateDto(model.Name, model.Category, model.SchemaJson, model.IsActive),
                cancellationToken);

            TempData["Message"] = "Şablon uğurla yaradıldı.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception) when (exception is ArgumentException or System.Text.Json.JsonException)
        {
            ModelState.AddModelError(nameof(model.SchemaJson), "JSON schema düzgün formatda deyil.");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var template = await templateService.GetByIdAsync(id, cancellationToken);
        if (template is null)
        {
            return NotFound();
        }

        return View(new TemplateFormViewModel
        {
            Id = template.Id,
            Name = template.Name,
            Category = template.Category,
            SchemaJson = template.SchemaJson,
            IsActive = template.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, TemplateFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.Id = id;
            return View(model);
        }

        try
        {
            var updated = await templateService.UpdateAsync(
                id,
                new UpdateTemplateDto(model.Name, model.Category, model.SchemaJson, model.IsActive),
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["Message"] = "Şablon uğurla yeniləndi.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception) when (exception is ArgumentException or System.Text.Json.JsonException)
        {
            model.Id = id;
            ModelState.AddModelError(nameof(model.SchemaJson), "JSON schema düzgün formatda deyil.");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var template = await templateService.GetByIdAsync(id, cancellationToken);
        return template is null ? NotFound() : View(template);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cancellationToken)
    {
        await templateService.DeleteAsync(id, cancellationToken);
        TempData["Message"] = "Şablon deaktiv siyahıdan silindi.";
        return RedirectToAction(nameof(Index));
    }
}
