using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microplex.Web.Data;
using Microplex.Web.Models;

namespace Microplex.Web.Controllers;

[Authorize(Roles = IdentitySeeder.SuperAdminRole)]
public sealed class InsightsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Sms(Guid? companyId, DateTime? from, DateTime? to)
    {
        var today = DateTime.Now.Date;
        var fromDate = (from ?? today).Date;
        var toDate = (to ?? today).Date;
        var toDateExclusive = toDate.AddDays(1);

        var query = db.SmsMessageLogs.AsNoTracking()
            .Include(x => x.Client)
            .Where(x => x.SentAtUtc >= fromDate && x.SentAtUtc < toDateExclusive);

        if (companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(x => x.ClientId == companyId.Value);

        var logs = await query.OrderByDescending(x => x.SentAtUtc).ToListAsync();

        var model = new SmsInsightViewModel
        {
            Companies = await db.Clients.AsNoTracking().Where(x => x.UsesSmsSolution).OrderBy(x => x.CompanyName).ToListAsync(),
            SelectedClientId = companyId,
            FromDate = fromDate,
            ToDate = toDate,
            Logs = logs,
            TotalSent = logs.Count(x => x.Success),
            TotalFailed = logs.Count(x => !x.Success)
        };
        return View(model);
    }
}
