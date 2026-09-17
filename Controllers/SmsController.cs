using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microplex.Web.Data;
using Microplex.Web.Services;

namespace Microplex.Web.Controllers;

[Authorize(Roles = IdentitySeeder.SuperAdminRole)]
public sealed class SmsController(ApplicationDbContext db, EmailSender emailSender) : Controller
{
    public async Task<IActionResult> Index()
    {
        var clients = await db.Clients.AsNoTracking()
            .Where(x => x.UsesSmsSolution)
            .OrderBy(x => x.CompanyName)
            .ToListAsync();
        return View(clients);
    }

    public IActionResult PreviewLowCreditAlert()
    {
        var html = LowCreditEmailTemplateBuilder.Build(LowCreditEmailTemplateBuilder.SampleCompanyName, LowCreditEmailTemplateBuilder.SampleBalance);
        return Content(html, "text/html");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCredit(Guid clientId, int amount)
    {
        if (amount <= 0)
        {
            TempData["Error"] = "Enter a credit amount greater than zero.";
            return RedirectToAction(nameof(Index));
        }

        var client = await db.Clients.FirstOrDefaultAsync(x => x.ClientId == clientId && x.UsesSmsSolution);
        if (client is null) return NotFound();

        client.SmsCredits += amount;
        await db.SaveChangesAsync();
        TempData["Success"] = $"{amount} credits added to {client.CompanyName}. New balance: {client.SmsCredits}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetCredit(Guid clientId)
    {
        var client = await db.Clients.FirstOrDefaultAsync(x => x.ClientId == clientId && x.UsesSmsSolution);
        if (client is null) return NotFound();

        client.SmsCredits = 0;
        await db.SaveChangesAsync();
        TempData["Success"] = $"{client.CompanyName}'s credit balance was reset to 0.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SendLowCreditAlert(Guid clientId)
    {
        var client = await db.Clients.FirstOrDefaultAsync(x => x.ClientId == clientId && x.UsesSmsSolution);
        if (client is null) return NotFound();

        var html = LowCreditEmailTemplateBuilder.Build(client.CompanyName, client.SmsCredits);
        try
        {
            await emailSender.SendAsync(client.Email, "Low SMS Credit Alert", html);
            TempData["Success"] = $"Low credit alert sent to {client.CompanyName} ({client.Email}).";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["Error"] = $"Failed to send the low credit alert to {client.CompanyName}. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }
}
