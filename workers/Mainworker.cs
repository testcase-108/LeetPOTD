using LeetPOTD.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LeetPOTD.Workers;

public sealed class MainWorker(
    ILogger<MainWorker> logger,
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

        // We will add LeetCode logic here next.

        await Task.CompletedTask;
    }

    private TimeSpan GetDelayUntilNextRun()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(
            _options.Zone);

        var nowUtc = DateTimeOffset.UtcNow;

        var localNow =
            TimeZoneInfo.ConvertTime(nowUtc, timeZone);

        var runTime = TimeSpan.Parse(_options.Time);

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