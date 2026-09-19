using Microsoft.AspNetCore.Mvc;
using Microplex.Web.Models;
using Microplex.Web.Services;

namespace Microplex.Web.Controllers;

public sealed class CosmeticsController(InternalEmailApiClient emailClient, IConfiguration configuration) : Controller
{
    public IActionResult Index()
    {
        return View(CosmeticCatalog.Items);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitQuoteRequest(CosmeticQuoteRequest request)
    {
        if (!ModelState.IsValid)
        {
            TempData["QuoteError"] = "Please fill in your name, WhatsApp number, address, and country.";
            return RedirectBack(request.ReturnUrl);
        }

        var notifyEmail = configuration["Inquiry:NotifyEmail"] ?? "nmssalman@outlook.com";

        try
        {
            var notificationHtml = CosmeticQuoteEmailTemplateBuilder.BuildNotification(
                request.Name, request.WhatsAppContact, request.Email, request.Address, request.Country, request.ItemName);
            await emailClient.SendAsync(notifyEmail, $"New quotation request: {request.ItemName}", notificationHtml);

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var thankYouHtml = CosmeticQuoteEmailTemplateBuilder.BuildThankYou(request.ItemName);
                await emailClient.SendAsync(request.Email, "Your quotation request — Microplex Cosmetics", thankYouHtml);
            }

            TempData["QuoteSuccess"] = "Thank you! We'll prepare your quotation and send it to your WhatsApp shortly.";
        }
        catch (Exception)
        {
            TempData["QuoteError"] = "Something went wrong submitting your request. Please try again or contact us directly.";
        }

        return RedirectBack(request.ReturnUrl);
    }

    private IActionResult RedirectBack(string? returnUrl)
        => Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction(nameof(Index));
}
