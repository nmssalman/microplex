using Microsoft.EntityFrameworkCore;
using Microplex.Web.Data;

namespace Microplex.Web.Services;

public sealed class ApiStatusTracker(ApplicationDbContext db, EmailSender emailSender, ILogger<ApiStatusTracker> logger)
{
    private const string AlertRecipient = "nmssalman@outlook.com";

    public async Task RecordSuccessAsync(string apiName, string methodName, CancellationToken cancellationToken = default)
    {
        var record = await GetOrCreateAsync(apiName, methodName, cancellationToken);
        record.ConsecutiveFailures = 0;
        record.Status = ApiHealthStatus.Operational;
        record.LastSuccessAtUtc = DateTime.UtcNow;
        record.LastCheckedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordFailureAsync(string apiName, string methodName, string errorMessage, CancellationToken cancellationToken = default)
    {
        var record = await GetOrCreateAsync(apiName, methodName, cancellationToken);
        record.ConsecutiveFailures += 1;
        record.LastFailureAtUtc = DateTime.UtcNow;
        record.LastCheckedAtUtc = DateTime.UtcNow;
        record.LastErrorMessage = errorMessage.Length > 500 ? errorMessage[..500] : errorMessage;

        var previousStatus = record.Status;
        record.Status = record.ConsecutiveFailures switch
        {
            0 => ApiHealthStatus.Operational,
            <= 3 => ApiHealthStatus.Warning,
            _ => ApiHealthStatus.Stopped
        };

        await db.SaveChangesAsync(cancellationToken);

        if (previousStatus != ApiHealthStatus.Stopped && record.Status == ApiHealthStatus.Stopped)
        {
            await SendUrgentAlertAsync(apiName, methodName, record.ConsecutiveFailures, record.LastErrorMessage, cancellationToken);
        }
    }

    private async Task<ApiStatusRecord> GetOrCreateAsync(string apiName, string methodName, CancellationToken cancellationToken)
    {
        var record = await db.ApiStatuses.FirstOrDefaultAsync(x => x.ApiName == apiName && x.MethodName == methodName, cancellationToken);
        if (record is not null) return record;

        record = new ApiStatusRecord { ApiName = apiName, MethodName = methodName };
        db.ApiStatuses.Add(record);
        return record;
    }

    private async Task SendUrgentAlertAsync(string apiName, string methodName, int consecutiveFailures, string? lastErrorMessage, CancellationToken cancellationToken)
    {
        try
        {
            // Sent via EmailSender (direct Brevo) rather than the internal Email Gateway API,
            // since the Email Gateway itself may be the thing that just got marked Stopped.
            var html = ApiStatusAlertEmailTemplateBuilder.Build(apiName, methodName, consecutiveFailures, lastErrorMessage);
            await emailSender.SendAsync(AlertRecipient, $"URGENT: {apiName} {methodName} is down", html, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send urgent API status alert for {ApiName} {MethodName}", apiName, methodName);
        }
    }
}
