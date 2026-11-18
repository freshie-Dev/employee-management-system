using EMS.Services.Interfaces;

namespace EMS.HostedServices;

/// <summary>
/// Opt-in daily job. Runs at a configured UTC clock time; examines prior
/// Pakistan-local work dates to pick up overnight shifts and missed runs.
/// Not a distributed scheduler: in multi-instance deployments, use a
/// durable job runner/queue with run-level coordination.
/// </summary>
public sealed class AttendanceBatchHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<AttendanceBatchHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var configuredTime = configuration["AttendanceBatch:RunAtUtc"] ?? "01:30";
        if (!TimeOnly.TryParseExact(configuredTime, "HH:mm", out var runTime))
            throw new InvalidOperationException("AttendanceBatch:RunAtUtc must be HH:mm.");

        var actorUserId = configuration.GetValue<int>("AttendanceBatch:SystemActorUserId");
        var lookback = Math.Clamp(configuration.GetValue<int>("AttendanceBatch:BackfillDays", 3), 1, 7);
        var maxEmployees = Math.Clamp(configuration.GetValue<int>("AttendanceBatch:MaxEmployees", 5000), 1, 5000);

        if (actorUserId <= 0)
            throw new InvalidOperationException(
                "AttendanceBatch:SystemActorUserId must identify a dedicated service account in UserAccounts.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var next = now.Date.Add(runTime.ToTimeSpan());
            if (next <= now) next = next.AddDays(1);

            await Task.Delay(next - now, stoppingToken);
            if (stoppingToken.IsCancellationRequested) break;

            var pakistanToday = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5));
            for (var daysAgo = lookback; daysAgo >= 1; daysAgo--)
            {
                if (stoppingToken.IsCancellationRequested) break;
                var workDate = pakistanToday.AddDays(-daysAgo);

                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var batchService = scope.ServiceProvider.GetRequiredService<IAttendanceBatchService>();
                    var result = await batchService.RunAsync(
                        workDate,
                        false,
                        actorUserId,
                        maxEmployees,
                        stoppingToken);

                    logger.LogInformation(
                        "Attendance batch date {WorkDate}: scanned={Scanned} processed={Processed} skipped={Skipped} failed={Failed} limitReached={LimitReached}",
                        workDate, result.Scanned, result.Processed,
                        result.Skipped, result.Failed, result.LimitReached);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Scheduled attendance batch failed on {WorkDate}", workDate);
                }
            }
        }
    }
}
