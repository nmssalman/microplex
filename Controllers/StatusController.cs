using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microplex.Web.Data;
using Microplex.Web.Models;

namespace Microplex.Web.Controllers;

[Route("status")]
public sealed class StatusController(ApplicationDbContext db) : Controller
{
    private static readonly (string ApiName, string MethodName, string Endpoint)[] MonitoredEndpoints =
    [
        ("SMS Gateway", "Send", "POST /api/sms/send"),
        ("SMS Gateway", "Balance", "GET /api/sms/balance"),
        ("Email Gateway", "Send", "POST /api/email/send"),
        ("Email Gateway", "Balance", "GET /api/email/balance"),
    ];

    public async Task<IActionResult> Index()
    {
        var records = await db.ApiStatuses.ToListAsync();
        var items = MonitoredEndpoints.Select(endpoint =>
        {
            var record = records.FirstOrDefault(x => x.ApiName == endpoint.ApiName && x.MethodName == endpoint.MethodName);
            return new ApiStatusItemViewModel
            {
                ApiName = endpoint.ApiName,
                MethodName = endpoint.MethodName,
                Endpoint = endpoint.Endpoint,
                Status = record?.Status ?? ApiHealthStatus.Operational,
                ConsecutiveFailures = record?.ConsecutiveFailures ?? 0,
                LastCheckedAtUtc = record?.LastCheckedAtUtc,
                LastSuccessAtUtc = record?.LastSuccessAtUtc
            };
        }).ToList();

        var overall = ApiHealthStatus.Operational;
        if (items.Any(x => x.Status == ApiHealthStatus.Stopped)) overall = ApiHealthStatus.Stopped;
        else if (items.Any(x => x.Status == ApiHealthStatus.Warning)) overall = ApiHealthStatus.Warning;

        return View(new StatusPageViewModel { Items = items, OverallStatus = overall });
    }
}
