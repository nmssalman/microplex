using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microplex.Web.Data;
using Microplex.Web.Models;

namespace Microplex.Web.Controllers;

[Authorize(Roles = IdentitySeeder.SuperAdminRole)]
public sealed class DashboardController(ApplicationDbContext db) : Controller
{
    private const int RecentClientCount = 5;

    public async Task<IActionResult> Index()
    {
        var model = new DashboardViewModel
        {
            TotalClients = await db.Clients.CountAsync(),
            RecentClients = await db.Clients.AsNoTracking()
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(RecentClientCount)
                .ToListAsync()
        };
        return View(model);
    }
}
