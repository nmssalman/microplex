using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microplex.Web.Data;
using Microplex.Web.Models;
using Microplex.Web.Services;

namespace Microplex.Web.Controllers;

[ApiController]
[Route("api/email")]
public sealed class EmailApiController(ApplicationDbContext db, EmailSender emailSender, ApiStatusTracker statusTracker) : ControllerBase
{
    private const string ApiName = "Email Gateway";

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        try
        {
            var client = await AuthenticateAsync();
            if (client is null) return Unauthorized(new { success = false, message = "Invalid or missing API key." });

            await statusTracker.RecordSuccessAsync(ApiName, "Balance");
            return Ok(new { success = true, companyName = client.CompanyName, balance = client.EmailCredits });
        }
        catch (Exception ex)
        {
            await statusTracker.RecordFailureAsync(ApiName, "Balance", ex.Message);
            return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
        }
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendEmailRequest request)
    {
        try
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
                var failureReason = $"Gateway rejected the request (HTTP {gatewayResult.StatusCode}).";
                await LogAttemptAsync(client, request, success: false, failureReason: failureReason, gatewayResponse: gatewayResult.ResponseBody);
                await statusTracker.RecordFailureAsync(ApiName, "Send", $"{failureReason} {gatewayResult.ResponseBody}");
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
            await statusTracker.RecordSuccessAsync(ApiName, "Send");

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
        catch (Exception ex)
        {
            await statusTracker.RecordFailureAsync(ApiName, "Send", ex.Message);
            return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
        }
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
