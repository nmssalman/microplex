using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microplex.Web.Data;
using Microplex.Web.Models;
using Microplex.Web.Services;

namespace Microplex.Web.Controllers;

[Authorize(Roles = IdentitySeeder.SuperAdminRole)]
public sealed class SystemMaintenanceController(ApplicationDbContext db, ApiStatusTracker statusTracker) : Controller
{
    public async Task<IActionResult> Management()
    {
        var records = await db.ApiStatuses.ToListAsync();
        var items = MonitoredApiEndpoints.All.Select(endpoint =>
        {
            var record = records.FirstOrDefault(x => x.ApiName == endpoint.ApiName && x.MethodName == endpoint.MethodName);
            return new ManagementItemViewModel
            {
                RecordId = record?.Id,
                ApiName = endpoint.ApiName,
                MethodName = endpoint.MethodName,
                Endpoint = endpoint.Endpoint,
                Status = record?.Status ?? ApiHealthStatus.Operational,
                ConsecutiveFailures = record?.ConsecutiveFailures ?? 0,
                LastCheckedAtUtc = record?.LastCheckedAtUtc,
                LastFailureAtUtc = record?.LastFailureAtUtc,
                LastErrorMessage = record?.LastErrorMessage
            };
        }).ToList();

        return View(items);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkResolved(int recordId)
    {
        var resolved = await statusTracker.ResolveAsync(recordId);
        TempData[resolved ? "Success" : "Error"] = resolved
            ? "Status marked as fixed. It will show Operational again."
            : "Could not find that status record.";
        return RedirectToAction(nameof(Management));
    }

    public async Task<IActionResult> Reports(string? apiName)
    {
        var query = db.ApiStatusIncidents.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(apiName))
            query = query.Where(x => x.ApiName == apiName);

        var incidents = await query.OrderByDescending(x => x.OccurredAtUtc).Take(200).ToListAsync();
        var apiNames = MonitoredApiEndpoints.All.Select(x => x.ApiName).Distinct().ToList();

        return View(new ReportsViewModel { Incidents = incidents, SelectedApiName = apiName, ApiNames = apiNames });
    }
}
