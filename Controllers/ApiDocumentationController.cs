using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microplex.Web.Data;
using Microplex.Web.Models;

namespace Microplex.Web.Controllers;

[Authorize(Roles = IdentitySeeder.SuperAdminRole)]
public sealed class ApiDocumentationController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Sms(Guid? clientId)
    {
        var clients = await db.Clients.AsNoTracking()
            .Where(x => x.UsesSmsSolution)
            .OrderBy(x => x.CompanyName)
            .ToListAsync();
        var model = new ApiDocumentationViewModel
        {
            Clients = clients,
            SelectedClientId = clientId,
            SelectedClient = clientId.HasValue ? clients.FirstOrDefault(x => x.ClientId == clientId.Value) : null
        };
        return View(model);
    }

    public async Task<IActionResult> Email(Guid? clientId)
    {
        var clients = await db.Clients.AsNoTracking()
            .Where(x => x.UsesEmailSolution)
            .OrderBy(x => x.CompanyName)
            .ToListAsync();
        var model = new ApiDocumentationViewModel
        {
            Clients = clients,
            SelectedClientId = clientId,
            SelectedClient = clientId.HasValue ? clients.FirstOrDefault(x => x.ClientId == clientId.Value) : null
        };
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateApiKey(Guid clientId)
    {
        var client = await db.Clients.FirstOrDefaultAsync(x => x.ClientId == clientId && x.UsesSmsSolution);
        if (client is null) return NotFound();

        client.SmsApiKey = GenerateKey("smsapi_");
        await db.SaveChangesAsync();
        TempData["Success"] = $"New API key generated for {client.CompanyName}.";
        return RedirectToAction(nameof(Sms), new { clientId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateEmailApiKey(Guid clientId)
    {
        var client = await db.Clients.FirstOrDefaultAsync(x => x.ClientId == clientId && x.UsesEmailSolution);
        if (client is null) return NotFound();

        client.EmailApiKey = GenerateKey("emailapi_");
        await db.SaveChangesAsync();
        TempData["Success"] = $"New API key generated for {client.CompanyName}.";
        return RedirectToAction(nameof(Email), new { clientId });
    }

    private static string GenerateKey(string prefix) => prefix + Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
}
