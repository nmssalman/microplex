using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microplex.Web.Data;

namespace Microplex.Web.Controllers;

[ApiController]
[Route("api/sms")]
public sealed class SmsApiController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        var client = await AuthenticateAsync();
        if (client is null) return Unauthorized(new { success = false, message = "Invalid or missing API key." });

        return Ok(new { success = true, companyName = client.CompanyName, balance = client.SmsCredits });
    }

    [HttpPost("report-sent")]
    public async Task<IActionResult> ReportSent()
    {
        var client = await AuthenticateAsync();
        if (client is null) return Unauthorized(new { success = false, message = "Invalid or missing API key." });

        if (client.SmsCredits <= 0)
            return BadRequest(new { success = false, message = "Insufficient SMS balance.", balance = client.SmsCredits });

        client.SmsCredits -= 1;
        await db.SaveChangesAsync();
        return Ok(new { success = true, companyName = client.CompanyName, balance = client.SmsCredits });
    }

    private async Task<Client?> AuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Api-Key", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
            return null;

        var key = apiKey.ToString();
        return await db.Clients.FirstOrDefaultAsync(x => x.SmsApiKey == key && x.UsesSmsSolution);
    }
}
