using CommunityStarter.Domain.Common;

namespace CommunityStarter.Domain.Operations;

public sealed class AuditEvent : Entity
{
    private AuditEvent() { }

    public Guid? CommunityId { get; private init; }
    public Guid? ActorAccountId { get; private init; }
    public string Kind { get; private init; } = string.Empty;
    public string TargetType { get; private init; } = string.Empty;
    public Guid? TargetId { get; private init; }
    public string SafeDetailsJson { get; private init; } = "{}";
    public string CorrelationId { get; private init; } = string.Empty;

    public static AuditEvent Record(
        Guid? communityId,
        Guid? actorAccountId,
        string kind,
        string targetType,
        Guid? targetId,
        string safeDetailsJson,
        string correlationId,
        DateTimeOffset now) => new()
        {
            Id = Guid.CreateVersion7(),
            CommunityId = communityId,
            ActorAccountId = actorAccountId,
            Kind = kind,
            TargetType = targetType,
            TargetId = targetId,
            SafeDetailsJson = safeDetailsJson,
            CorrelationId = correlationId,
            CreatedAt = now,
            UpdatedAt = now
        };
}

public sealed class OutboxMessage : Entity
{
    private OutboxMessage() { }

    public string Kind { get; private init; } = string.Empty;
    public string PayloadJson { get; private init; } = "{}";
    public DateTimeOffset? ProcessedAt { get; private set; }

    public static OutboxMessage Create(string kind, string payloadJson, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        Kind = kind,
        PayloadJson = payloadJson,
        CreatedAt = now,
        UpdatedAt = now
    };

    public void MarkProcessed(DateTimeOffset now)
    {
        ProcessedAt = now;
        Touch(now);
    }
}

public enum JobStatus
{
    Ready,
    Leased,
    Retrying,
    Succeeded,
    Cancelled,
    Terminal
}

public sealed class DurableJob : Entity
{
    private DurableJob() { }

    public string Kind { get; private init; } = string.Empty;
    public string PayloadJson { get; private init; } = "{}";
    public JobStatus Status { get; private set; }
    public int Attempt { get; private set; }
    public DateTimeOffset AvailableAt { get; private set; }
    public string? LeaseOwner { get; private set; }
    public DateTimeOffset? LeaseExpiresAt { get; private set; }
    public string? SafeError { get; private set; }

    public static DurableJob Enqueue(string kind, string payloadJson, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        Kind = kind,
        PayloadJson = payloadJson,
        Status = JobStatus.Ready,
        AvailableAt = now,
        CreatedAt = now,
        UpdatedAt = now
    };

    public void Lease(string owner, DateTimeOffset now, TimeSpan duration)
    {
        if (Status is not (JobStatus.Ready or JobStatus.Retrying) || AvailableAt > now)
        {
            throw new DomainException("job_not_available", "The job is not available for execution.");
        }

        LeaseOwner = owner;
        LeaseExpiresAt = now.Add(duration);
        Status = JobStatus.Leased;
        Attempt++;
        Touch(now);
    }

    public void Complete(DateTimeOffset now)
    {
        if (Status != JobStatus.Leased)
        {
            throw new DomainException("job_not_leased", "Only a leased job can be completed.");
        }

        Status = JobStatus.Succeeded;
        LeaseOwner = null;
        LeaseExpiresAt = null;
        Touch(now);
    }

    public void RecoverExpiredLease(DateTimeOffset now)
    {
        if (Status != JobStatus.Leased || LeaseExpiresAt is null || LeaseExpiresAt > now)
        {
            throw new DomainException("job_lease_active", "The job lease has not expired.");
        }

        Status = JobStatus.Retrying;
        AvailableAt = now;
        LeaseOwner = null;
        LeaseExpiresAt = null;
        Touch(now);
    }

    public void Fail(string safeError, DateTimeOffset now, int maximumAttempts)
    {
        if (Status != JobStatus.Leased)
        {
            throw new DomainException("job_not_leased", "Only a leased job can fail.");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(maximumAttempts, 1);
        SafeError = safeError;
        LeaseOwner = null;
        LeaseExpiresAt = null;
        if (Attempt >= maximumAttempts)
        {
            Status = JobStatus.Terminal;
        }
        else
        {
            Status = JobStatus.Retrying;
            // Stable per job and attempt so retry behavior is reproducible in tests and replays.
            byte[] identity = Id.ToByteArray();
            double jitterSeconds = (identity[Attempt % identity.Length] + Attempt) % 100 / 100d;
            AvailableAt = now.AddSeconds(Math.Min(900, Math.Pow(2, Attempt)) + jitterSeconds);
        }

        Touch(now);
    }
}
