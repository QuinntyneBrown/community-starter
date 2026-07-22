using CommunityStarter.Domain.Common;
using CommunityStarter.Domain.Operations;
using CommunityStarter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommunityStarter.Infrastructure.Jobs;

public sealed class DurableJobOptions
{
    public const string SectionName = "Jobs";
    public int BatchSize { get; init; } = 20;
    public int MaximumAttempts { get; init; } = 8;
    public int PollIntervalMilliseconds { get; init; } = 1_000;
    public int LeaseSeconds { get; init; } = 60;
}

public interface IDurableJobDispatcher
{
    ValueTask DispatchAsync(DurableJob job, CancellationToken cancellationToken);
}

public sealed class DurableJobDispatcher(
    IHostEnvironment environment,
    ILogger<DurableJobDispatcher> logger) : IDurableJobDispatcher
{
    private static readonly Action<ILogger, Guid, string, Exception?> DispatchedLog = LoggerMessage.Define<Guid, string>(
        LogLevel.Information,
        new EventId(2101, "DurableJobDispatched"),
        "Dispatched durable job {JobId} of kind {JobKind}.");

    public ValueTask DispatchAsync(DurableJob job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        cancellationToken.ThrowIfCancellationRequested();
        if (job.Kind != "feature.project" && !job.Kind.StartsWith("outbox.", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"No durable job adapter is registered for '{job.Kind}'.");
        }

        if (environment.IsProduction())
        {
            throw new InvalidOperationException(
                $"A production adapter must be registered before dispatching '{job.Kind}'.");
        }

        // Provider-specific email, object-storage, search, and realtime adapters replace this boundary.
        DispatchedLog(logger, job.Id, job.Kind, null);
        return ValueTask.CompletedTask;
    }
}

public sealed class DurableJobWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<DurableJobOptions> options,
    IHostEnvironment environment,
    ILogger<DurableJobWorker> logger) : BackgroundService
{
    private static readonly Action<ILogger, Guid, Exception?> FailedLog = LoggerMessage.Define<Guid>(
        LogLevel.Warning,
        new EventId(2102, "DurableJobFailed"),
        "Durable job {JobId} failed and will follow its retry policy.");
    private readonly DurableJobOptions settings = options.Value;
    private readonly string leaseOwner = $"{environment.ApplicationName}:{Environment.MachineName}:{Environment.ProcessId}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan interval = TimeSpan.FromMilliseconds(Math.Max(100, settings.PollIntervalMilliseconds));
        using PeriodicTimer timer = new(interval);
        do
        {
            await ProcessBatchAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        CommunityDbContext dbContext = scope.ServiceProvider.GetRequiredService<CommunityDbContext>();
        IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();
        IDurableJobDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<IDurableJobDispatcher>();
        DateTimeOffset now = clock.UtcNow;

        List<OutboxMessage> outbox = await dbContext.OutboxMessages
            .Where(message => message.ProcessedAt == null)
            .OrderBy(message => message.CreatedAt)
            .Take(Math.Max(1, settings.BatchSize))
            .ToListAsync(cancellationToken);
        foreach (OutboxMessage message in outbox)
        {
            dbContext.Jobs.Add(DurableJob.Enqueue($"outbox.{message.Kind}", message.PayloadJson, now));
            message.MarkProcessed(now);
        }

        List<DurableJob> expired = await dbContext.Jobs
            .Where(job => job.Status == JobStatus.Leased && job.LeaseExpiresAt <= now)
            .Take(Math.Max(1, settings.BatchSize))
            .ToListAsync(cancellationToken);
        foreach (DurableJob job in expired)
        {
            job.RecoverExpiredLease(now);
        }

        if (outbox.Count > 0 || expired.Count > 0)
        {
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return;
            }
        }

        List<DurableJob> jobs = await dbContext.Jobs
            .Where(job =>
                (job.Status == JobStatus.Ready || job.Status == JobStatus.Retrying) &&
                job.AvailableAt <= now)
            .OrderBy(job => job.AvailableAt)
            .ThenBy(job => job.Id)
            .Take(Math.Max(1, settings.BatchSize))
            .ToListAsync(cancellationToken);

        foreach (DurableJob job in jobs)
        {
            try
            {
                job.Lease(leaseOwner, now, TimeSpan.FromSeconds(Math.Max(5, settings.LeaseSeconds)));
                await dbContext.SaveChangesAsync(cancellationToken);
                await dispatcher.DispatchAsync(job, cancellationToken);
                job.Complete(clock.UtcNow);
            }
            catch (DbUpdateConcurrencyException)
            {
                dbContext.Entry(job).State = EntityState.Detached;
                continue;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                FailedLog(logger, job.Id, exception);
                job.Fail("provider_dispatch_failed", clock.UtcNow, Math.Max(1, settings.MaximumAttempts));
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
