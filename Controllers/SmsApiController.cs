using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microplex.Web.Data;
using Microplex.Web.Models;
using Microplex.Web.Services;

namespace Microplex.Web.Controllers;

[ApiController]
[Route("api/sms")]
public sealed class SmsApiController(ApplicationDbContext db, SmsGatewayClient smsGateway) : ControllerBase
{
    private static readonly string[] AllowedMessageTypes = ["plain", "unicode"];

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        var client = await AuthenticateAsync();
        if (client is null) return Unauthorized(new { success = false, message = "Invalid or missing API key." });

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
        {
            await LogAttemptAsync(client, request, success: false, failureReason: "Insufficient SMS balance.", gatewayResponse: null);
            return BadRequest(new { success = false, message = "Insufficient SMS balance.", balance = client.SmsCredits });
        }

        var gatewayResult = await smsGateway.SendAsync(request.Recipient, request.SenderId, request.Type, request.Message);
        if (!gatewayResult.Success)
        {
            await LogAttemptAsync(client, request, success: false, failureReason: $"Gateway rejected the request (HTTP {gatewayResult.StatusCode}).", gatewayResponse: gatewayResult.ResponseBody);
            return StatusCode(502, new
            {
                success = false,
                message = "The SMS gateway rejected the request.",
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
            balance = client.SmsCredits,
            gatewayResponse = gatewayResult.ResponseBody
        });
    }

    private async Task DeductOneCreditAsync(Client client)
    {
        client.SmsCredits -= 1;
        await db.SaveChangesAsync();
    }

    private async Task LogAttemptAsync(Client client, SendSmsRequest request, bool success, string? failureReason, string? gatewayResponse)
    {
        db.SmsMessageLogs.Add(new SmsMessageLog
        {
            ClientId = client.ClientId,
            Recipient = request.Recipient!,
            SenderId = request.SenderId!,
            Type = request.Type!,
            Message = request.Message!,
            Success = success,
            FailureReason = failureReason,
            GatewayResponse = gatewayResponse,
            BalanceAfter = client.SmsCredits,
            SentAtUtc = DateTime.UtcNow
        });
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
