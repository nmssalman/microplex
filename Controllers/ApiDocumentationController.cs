using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microplex.Web.Data;

namespace Microplex.Web.Controllers;

[Authorize(Roles = IdentitySeeder.SuperAdminRole)]
public sealed class ApiDocumentationController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Sms()
    {
        var clients = await db.Clients.AsNoTracking()
            .Where(x => x.UsesSmsSolution)
            .OrderBy(x => x.CompanyName)
            .ToListAsync();
        return View(clients);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateApiKey(Guid clientId)
    {
        var client = await db.Clients.FirstOrDefaultAsync(x => x.ClientId == clientId && x.UsesSmsSolution);
        if (client is null) return NotFound();

        client.SmsApiKey = GenerateKey();
        await db.SaveChangesAsync();
        TempData["Success"] = $"New API key generated for {client.CompanyName}.";
        return RedirectToAction(nameof(Sms));
    }

    private static string GenerateKey() => "smsapi_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
}
