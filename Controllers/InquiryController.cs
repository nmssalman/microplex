using Microsoft.AspNetCore.Mvc;
using Microplex.Web.Models;
using Microplex.Web.Services;

namespace Microplex.Web.Controllers;

public sealed class InquiryController(InternalEmailApiClient emailClient, IConfiguration configuration) : Controller
{
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(ServiceInquiryRequest request)
    {
        if (!ModelState.IsValid)
        {
            TempData["InquiryError"] = "Please enter a valid email address.";
            return RedirectBack(request.ReturnUrl);
        }

        var notifyEmail = configuration["Inquiry:NotifyEmail"] ?? "nmssalman@outlook.com";

        try
        {
            var thankYouHtml = InquiryEmailTemplateBuilder.BuildThankYou(request.Service);
            await emailClient.SendAsync(request.Email, "Thank you for contacting Microplex", thankYouHtml);

            var notificationHtml = InquiryEmailTemplateBuilder.BuildNotification(request.Email, request.Service);
            await emailClient.SendAsync(notifyEmail, $"New {request.Service} inquiry", notificationHtml);

            TempData["InquirySuccess"] = "Thanks for reaching out! We've sent you a confirmation and our team will be in touch shortly.";
        }
        catch (Exception)
        {
            TempData["InquiryError"] = "Something went wrong sending your inquiry. Please try again or contact us directly.";
        }

        return RedirectBack(request.ReturnUrl);
    }

    private IActionResult RedirectBack(string? returnUrl)
        => Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction("Solutions", "Home");
}
