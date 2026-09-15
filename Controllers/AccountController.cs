using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microplex.Web.Data;
using Microplex.Web.Models;

namespace Microplex.Web.Controllers;

public sealed class AccountController(SignInManager<ApplicationUser> signInManager) : Controller
{
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var result = await signInManager.PasswordSignInAsync(model.Username.Trim(), model.Password,
            model.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.IsLockedOut
                ? "This account is temporarily locked. Please try again later."
                : "The username or password is incorrect.");
            return View(model);
        }

        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction("Index", "Dashboard");
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }
}
