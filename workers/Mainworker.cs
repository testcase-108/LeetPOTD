using LeetPOTD.Configuration;
using LeetPOTD.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LeetPOTD.Workers;

public sealed class MainWorker(
    ILogger<MainWorker> logger,
    IServiceScopeFactory scopeFactory,
    IOptions<Timezone> options) : BackgroundService
{
    private readonly Timezone _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "LeetCode POTD worker started. Timezone: {TimeZone}, RunTime: {RunTime}",
            _options.Zone,
            _options.Time);

        while (!cancellationToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun();

            logger.LogInformation(
                "Next POTD run in {Delay}.",
                delay);

            try
            {
                await Task.Delay(delay, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await ProcessPotdAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error while processing LeetCode POTD.");
            }
        }

        logger.LogInformation("LeetCode POTD worker stopped.");
    }

    private async Task ProcessPotdAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "========================================");

        logger.LogInformation(
            "POTD job triggered at {Time}",
            DateTimeOffset.Now);

        using var scope = scopeFactory.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<IPotdOrchestrator>();

        var run = await orchestrator.RunAsync(cancellationToken);

        logger.LogInformation(
            "POTD job completed with status {Status}. RunId: {RunId}",
            run.Status,
            run.Id);
    }

    private TimeSpan GetDelayUntilNextRun()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(
            _options.Zone ?? "Asia/Kolkata");

        var nowUtc = DateTimeOffset.UtcNow;

        var localNow =
            TimeZoneInfo.ConvertTime(nowUtc, timeZone);

        var runTime = TimeSpan.Parse(_options.Time ?? "00:00");

        var nextRunLocal = localNow.Date + runTime;

        if (nextRunLocal <= localNow.DateTime)
        {
            nextRunLocal = nextRunLocal.AddDays(1);
        }

        var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(
            nextRunLocal,
            timeZone);

        return nextRunUtc - nowUtc.UtcDateTime;
    }
}
