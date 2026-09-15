using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microplex.Web.Data;

namespace Microplex.Web.Controllers;

[Authorize(Roles = IdentitySeeder.SuperAdminRole)]
public sealed class ClientsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? search)
    {
        var query = db.Clients.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.CompanyName.Contains(term) || x.Email.Contains(term) ||
                                     x.Mobile.Contains(term) || x.Address.Contains(term));
        }
        ViewData["Search"] = search;
        return View(await query.OrderBy(x => x.CompanyName).ToListAsync());
    }

    public IActionResult Create() => View(new Client());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CompanyName,Address,Email,Mobile,Website,UsesSoftwareSolution,UsesEmailSolution,UsesSmsSolution")] Client client)
    {
        if (!string.IsNullOrWhiteSpace(client.Website) && !client.Website.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !client.Website.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            client.Website = $"https://{client.Website.Trim()}";
            ModelState.Remove(nameof(Client.Website));
            if (!Uri.TryCreate(client.Website, UriKind.Absolute, out _)) ModelState.AddModelError(nameof(Client.Website), "Enter a valid website address.");
        }
        if (!ModelState.IsValid) return View(client);

        client.ClientId = Guid.NewGuid();
        client.CreatedAtUtc = DateTime.UtcNow;
        db.Clients.Add(client);
        await db.SaveChangesAsync();
        TempData["Success"] = $"{client.CompanyName} was registered successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var client = await db.Clients.FindAsync(id);
        return client is null ? NotFound() : View(client);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("CompanyName,Address,Email,Mobile,Website,UsesSoftwareSolution,UsesEmailSolution,UsesSmsSolution")] Client input)
    {
        var client = await db.Clients.FindAsync(id);
        if (client is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(input.Website) && !input.Website.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !input.Website.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            input.Website = $"https://{input.Website.Trim()}";
            ModelState.Remove(nameof(Client.Website));
            if (!Uri.TryCreate(input.Website, UriKind.Absolute, out _)) ModelState.AddModelError(nameof(Client.Website), "Enter a valid website address.");
        }
        if (!ModelState.IsValid) { input.ClientId = id; return View(input); }
        client.CompanyName = input.CompanyName; client.Address = input.Address; client.Email = input.Email; client.Mobile = input.Mobile; client.Website = input.Website;
        client.UsesSoftwareSolution = input.UsesSoftwareSolution; client.UsesEmailSolution = input.UsesEmailSolution; client.UsesSmsSolution = input.UsesSmsSolution;
        await db.SaveChangesAsync();
        TempData["Success"] = $"{client.CompanyName} was updated successfully.";
        return RedirectToAction(nameof(Index));
    }
}
