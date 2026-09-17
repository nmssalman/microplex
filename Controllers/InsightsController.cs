using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microplex.Web.Data;
using Microplex.Web.Models;
using Microplex.Web.Utilities;

namespace Microplex.Web.Controllers;

[Authorize(Roles = IdentitySeeder.SuperAdminRole)]
public sealed class InsightsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Sms(Guid? companyId, DateTime? from, DateTime? to)
    {
        var (fromDate, toDate, fromUtc, toUtcExclusive) = ResolveRange(from, to);

        var query = db.SmsMessageLogs.AsNoTracking()
            .Include(x => x.Client)
            .Where(x => x.SentAtUtc >= fromUtc && x.SentAtUtc < toUtcExclusive);

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

    public async Task<IActionResult> Email(Guid? companyId, DateTime? from, DateTime? to)
    {
        var (fromDate, toDate, fromUtc, toUtcExclusive) = ResolveRange(from, to);

        var query = db.EmailMessageLogs.AsNoTracking()
            .Include(x => x.Client)
            .Where(x => x.SentAtUtc >= fromUtc && x.SentAtUtc < toUtcExclusive);

        if (companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(x => x.ClientId == companyId.Value);

        var logs = await query.OrderByDescending(x => x.SentAtUtc).ToListAsync();

        var model = new EmailInsightViewModel
        {
            Companies = await db.Clients.AsNoTracking().Where(x => x.UsesEmailSolution).OrderBy(x => x.CompanyName).ToListAsync(),
            SelectedClientId = companyId,
            FromDate = fromDate,
            ToDate = toDate,
            Logs = logs,
            TotalSent = logs.Count(x => x.Success),
            TotalFailed = logs.Count(x => !x.Success)
        };
        return View(model);
    }

    private static (DateTime FromDate, DateTime ToDate, DateTime FromUtc, DateTime ToUtcExclusive) ResolveRange(DateTime? from, DateTime? to)
    {
        var today = SriLankaTime.Now.Date;
        var fromDate = (from ?? today).Date;
        var toDate = (to ?? today).Date;

        // SentAtUtc is stored in UTC, so the Sri Lanka local day boundaries must be
        // converted to UTC before filtering - comparing them directly would be off by 5:30.
        var fromUtc = SriLankaTime.ToUtc(fromDate);
        var toUtcExclusive = SriLankaTime.ToUtc(toDate.AddDays(1));
        return (fromDate, toDate, fromUtc, toUtcExclusive);
    }
}
