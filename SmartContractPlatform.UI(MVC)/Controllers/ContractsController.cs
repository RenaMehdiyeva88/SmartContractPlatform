using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartContractPlatform.BLL.DTOs;
using SmartContractPlatform.BLL.Services.Interfaces;
using SmartContractPlatform.MVC.Models;
using SmartContractPlatform.DAL.Enums;
using System.Security.Claims;

namespace SmartContractPlatform.MVC.Controllers;

[Authorize]
public class ContractsController(
    IContractService contractService,
    ITemplateService templateService,
    ISigningService signingService,
    IDocumentService documentService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var contracts = await contractService.GetMyContractsAsync(GetCurrentUserId(), cancellationToken);
        return View(contracts);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var contract = await contractService.GetByIdAsync(id, GetCurrentUserId(), GetIpAddress(), cancellationToken);
        if (contract is null)
        {
            return NotFound();
        }

        var document = await documentService.GetByContractIdAsync(id, GetCurrentUserId(), cancellationToken);
        ViewBag.HasDocument = document is not null;
        ViewBag.CurrentUserId = GetCurrentUserId();

        return View(contract);
    }

    [HttpGet]
    public async Task<IActionResult> Create(string? templateId, CancellationToken cancellationToken)
    {
        var templates = await templateService.GetAllAsync(cancellationToken);
        ViewBag.Templates = templates;

        if (!Guid.TryParse(templateId, out var parsedTemplateId))
        {
            return View(new CreateContractViewModel());
        }

        if (!templates.Any(template => template.Id == parsedTemplateId))
        {
            TempData["Message"] = "Seçilmiş şablon tapılmadı. Zəhmət olmasa şablonu yenidən seçin.";
            return View(new CreateContractViewModel());
        }

        var existingContract = await contractService.GetExistingForTemplateAsync(parsedTemplateId, GetCurrentUserId(), cancellationToken);
        if (existingContract is not null)
        {
            return RedirectToAction(nameof(Details), new { id = existingContract.Id });
        }

        return View(new CreateContractViewModel { TemplateId = parsedTemplateId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateContractViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Templates = await templateService.GetAllAsync(cancellationToken);
            return View(model);
        }

        var dto = new CreateContractDto(
            model.TemplateId,
            model.DataJson,
            model.OtherPartyIdentificationNumber,
            model.CreatorRole,
            model.OtherPartyRole,
            model.OtherPartyFullName,
            model.OtherPartyContactEmail,
            model.OtherPartyContactPhone);

        try
        {
            var contract = await contractService.CreateAsync(dto, GetCurrentUserId(), GetIpAddress(), cancellationToken);
            return RedirectToAction(nameof(Details), new { id = contract.Id });
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            ViewBag.Templates = await templateService.GetAllAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sign(Guid contractId, CancellationToken cancellationToken)
    {
        try
        {
            await signingService.SignAsync(
                contractId,
                new SignContractDto(SignatureType.Digital),
                GetCurrentUserId(),
                GetIpAddress(),
                cancellationToken);

            TempData["Message"] = "Müqavilə uğurla imzalandı.";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            TempData["Message"] = $"Müqavilə imzalana bilmədi: {exception.Message}";
        }

        return RedirectToAction(nameof(Details), new { id = contractId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id, string reason, CancellationToken cancellationToken)
    {
        try
        {
            var rejected = await contractService.RejectAsync(id, new RejectContractDto(reason), GetCurrentUserId(), GetIpAddress(), cancellationToken);
            TempData["Message"] = rejected
                ? "Müqavilə rədd edildi."
                : "Müqavilə rədd edilə bilmədi.";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            TempData["Message"] = $"Müqavilə rədd edilə bilmədi: {exception.Message}";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GeneratePdf(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var document = await documentService.GenerateForContractAsync(id, GetCurrentUserId(), cancellationToken);
            if (document is null)
            {
                TempData["Message"] = "PDF generasiya edilə bilmədi: müqavilə tapılmadı və ya giriş icazəniz yoxdur.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var file = await documentService.DownloadByContractIdAsync(id, GetCurrentUserId(), cancellationToken);
            if (file is null)
            {
                TempData["Message"] = "PDF generasiya edildi, amma fayl endirilə bilmədi.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return File(file.Content, "application/pdf", file.FileName);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            TempData["Message"] = $"PDF generasiya edilə bilmədi: {exception.Message}";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken cancellationToken)
    {
        var file = await documentService.DownloadByContractIdAsync(id, GetCurrentUserId(), cancellationToken);
        return file is null ? NotFound() : File(file.Content, file.ContentType, file.FileName);
    }

    private Guid GetCurrentUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string GetIpAddress()
        => HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
}
