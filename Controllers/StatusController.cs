using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microplex.Web.Data;
using Microplex.Web.Models;
using Microplex.Web.Services;

namespace Microplex.Web.Controllers;

[Route("status")]
public sealed class StatusController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var records = await db.ApiStatuses.ToListAsync();
        var items = MonitoredApiEndpoints.All.Select(endpoint =>
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
