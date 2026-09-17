using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microplex.Web.Data;
using Microplex.Web.Models;
using Microplex.Web.Services;

namespace Microplex.Web.Controllers;

[ApiController]
[Route("api/email")]
public sealed class EmailApiController(ApplicationDbContext db, EmailSender emailSender) : ControllerBase
{
    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        var client = await AuthenticateAsync();
        if (client is null) return Unauthorized(new { success = false, message = "Invalid or missing API key." });

        return Ok(new { success = true, companyName = client.CompanyName, balance = client.EmailCredits });
    }

    [HttpPost("report-sent")]
    public async Task<IActionResult> ReportSent()
    {
        var client = await AuthenticateAsync();
        if (client is null) return Unauthorized(new { success = false, message = "Invalid or missing API key." });

        if (client.EmailCredits <= 0)
            return BadRequest(new { success = false, message = "Insufficient Email balance.", balance = client.EmailCredits });

        await DeductOneCreditAsync(client);
        return Ok(new { success = true, companyName = client.CompanyName, balance = client.EmailCredits });
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendEmailRequest request)
    {
        var client = await AuthenticateAsync(request.ApiToken);
        if (client is null) return Unauthorized(new { success = false, message = "Invalid or missing API token." });

        if (string.IsNullOrWhiteSpace(request.Recipient))
            return BadRequest(new { success = false, message = "recipient is required." });
        if (string.IsNullOrWhiteSpace(request.Subject))
            return BadRequest(new { success = false, message = "subject is required." });
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { success = false, message = "message is required." });

        if (client.EmailCredits <= 0)
        {
            await LogAttemptAsync(client, request, success: false, failureReason: "Insufficient Email balance.", gatewayResponse: null);
            return BadRequest(new { success = false, message = "Insufficient Email balance.", balance = client.EmailCredits });
        }

        var senderName = string.IsNullOrWhiteSpace(request.SenderName) ? client.CompanyName : request.SenderName;
        var gatewayResult = await emailSender.SendAsync(request.Recipient, request.RecipientName, request.Subject, request.Message, senderName);
        if (!gatewayResult.Success)
        {
            await LogAttemptAsync(client, request, success: false, failureReason: $"Gateway rejected the request (HTTP {gatewayResult.StatusCode}).", gatewayResponse: gatewayResult.ResponseBody);
            return StatusCode(502, new
            {
                success = false,
                message = "The email gateway rejected the request.",
                gatewayStatusCode = gatewayResult.StatusCode,
                gatewayResponse = gatewayResult.ResponseBody
            });
        }

        await DeductOneCreditAsync(client);
        await LogAttemptAsync(client, request, success: true, failureReason: null, gatewayResponse: gatewayResult.ResponseBody);

        return Ok(new
        {
            success = true,
            message_id = Guid.NewGuid().ToString("N"),
            recipient = request.Recipient,
            status = "sent",
            balance = client.EmailCredits,
            gatewayResponse = gatewayResult.ResponseBody
        });
    }

    private async Task DeductOneCreditAsync(Client client)
    {
        client.EmailCredits -= 1;
        await db.SaveChangesAsync();
    }

    private async Task LogAttemptAsync(Client client, SendEmailRequest request, bool success, string? failureReason, string? gatewayResponse)
    {
        db.EmailMessageLogs.Add(new EmailMessageLog
        {
            ClientId = client.ClientId,
            Recipient = request.Recipient!,
            RecipientName = request.RecipientName,
            Subject = request.Subject!,
            Message = request.Message!,
            Success = success,
            FailureReason = failureReason,
            GatewayResponse = gatewayResponse,
            BalanceAfter = client.EmailCredits,
            SentAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private async Task<Client?> AuthenticateAsync(string? bodyApiToken = null)
    {
        var key = ResolveApiKey(bodyApiToken);
        if (string.IsNullOrWhiteSpace(key)) return null;

        return await db.Clients.FirstOrDefaultAsync(x => x.EmailApiKey == key && x.UsesEmailSolution);
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
