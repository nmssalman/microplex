using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microplex.Web.Data;
using Microplex.Web.Models;

namespace Microplex.Web.Controllers;

[ApiController]
[Route("api/sms")]
public sealed class SmsApiController(ApplicationDbContext db) : ControllerBase
{
    private static readonly string[] AllowedMessageTypes = ["plain", "unicode"];

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

        await DeductOneCreditAsync(client);
        return Ok(new { success = true, companyName = client.CompanyName, balance = client.SmsCredits });
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendSmsRequest request)
    {
        var client = await AuthenticateAsync(request.ApiToken);
        if (client is null) return Unauthorized(new { success = false, message = "Invalid or missing API token." });

        if (string.IsNullOrWhiteSpace(request.Recipient))
            return BadRequest(new { success = false, message = "recipient is required." });
        if (string.IsNullOrWhiteSpace(request.SenderId))
            return BadRequest(new { success = false, message = "sender_id is required." });
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { success = false, message = "message is required." });
        if (string.IsNullOrWhiteSpace(request.Type) || !AllowedMessageTypes.Contains(request.Type, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { success = false, message = "type must be 'plain' or 'unicode'." });

        if (client.SmsCredits <= 0)
            return BadRequest(new { success = false, message = "Insufficient SMS balance.", balance = client.SmsCredits });

        await DeductOneCreditAsync(client);

        return Ok(new
        {
            success = true,
            message_id = Guid.NewGuid().ToString("N"),
            recipient = request.Recipient,
            status = "sent",
            balance = client.SmsCredits
        });
    }

    private async Task DeductOneCreditAsync(Client client)
    {
        client.SmsCredits -= 1;
        await db.SaveChangesAsync();
    }

    private async Task<Client?> AuthenticateAsync(string? bodyApiToken = null)
    {
        var key = ResolveApiKey(bodyApiToken);
        if (string.IsNullOrWhiteSpace(key)) return null;

        return await db.Clients.FirstOrDefaultAsync(x => x.SmsApiKey == key && x.UsesSmsSolution);
    }

    private string? ResolveApiKey(string? bodyApiToken)
    {
        if (Request.Headers.TryGetValue("api_token", out var headerToken) && !string.IsNullOrWhiteSpace(headerToken))
            return headerToken.ToString();
        if (Request.Headers.TryGetValue("X-Api-Key", out var legacyHeaderToken) && !string.IsNullOrWhiteSpace(legacyHeaderToken))
            return legacyHeaderToken.ToString();

        return string.IsNullOrWhiteSpace(bodyApiToken) ? null : bodyApiToken;
    }
}
