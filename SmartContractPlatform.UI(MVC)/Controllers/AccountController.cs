using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartContractPlatform.BLL.DTOs;
using SmartContractPlatform.BLL.Services.Interfaces;
using SmartContractPlatform.MVC.Models;
using System.Security.Claims;

namespace SmartContractPlatform.MVC.Controllers;

public class AccountController(IAuthService authService) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await authService.LoginAsync(new LoginRequestDto(model.Email, model.Password));

        if (result is null)
        {
            ModelState.AddModelError(string.Empty, "Email və ya şifrə yanlışdır.");
            return View(model);
        }

        await SignInAsync(result.User);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegisterViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var dto = new RegisterRequestDto(
            model.UserType,
            model.FinCode,
            model.Voen,
            model.Name,
            model.Surname,
            model.FatherName,
            model.CompanyName,
            model.RepresentativeName,
            model.Email,
            model.Phone,
            model.Password);

        var result = await authService.RegisterAsync(dto);

        if (!result.Succeeded || result.Response is null)
        {
            var errors = result.Errors
                .Where(error => !string.IsNullOrWhiteSpace(error))
                .DefaultIfEmpty("Qeydiyyat tamamlanmadı. Məlumatları yoxlayıb yenidən cəhd edin.");

            foreach (var error in errors)
            {
                ModelState.AddModelError(string.Empty, error!);
            }

            return View(model);
        }

        await SignInAsync(result.Response.User);
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private async Task SignInAsync(UserDto user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });
    }
}
