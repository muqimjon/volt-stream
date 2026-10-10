namespace VoltStream.Infrastructure.Printing;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Features.PrintJobs;
using VoltStream.Domain.Enums;

public class PrintJobWatchdog(
    IServiceProvider services,
    ILogger<PrintJobWatchdog> logger) : BackgroundService
{
    private static readonly TimeSpan interval = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan timeout = TimeSpan.FromMinutes(3);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);
                await SweepAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Chop etish navbatini tekshirishda xatolik");
            }
        }
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var notifier = scope.ServiceProvider.GetRequiredService<IPrintJobNotifier>();
        var deadline = DateTimeOffset.UtcNow - timeout;

        var stuck = await context.PrintJobs
            .Where(job => job.Status == PrintJobStatus.Printing && job.ClaimedAt < deadline)
            .ToListAsync(cancellationToken);

        foreach (var job in stuck)
        {
            await job.RequeueOrFailAsync("Agent belgilangan vaqtda javob bermadi.", context, notifier, cancellationToken);
            logger.LogWarning("Chop etish so'rovi {JobId} agentdan qaytarib olindi ({Status})", job.Id, job.Status);
        }
    }
}
