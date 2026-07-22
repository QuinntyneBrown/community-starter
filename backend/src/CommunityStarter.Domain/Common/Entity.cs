namespace CommunityStarter.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected init; } = Guid.CreateVersion7();
    public long Version { get; protected set; }
    public DateTimeOffset CreatedAt { get; protected init; }
    public DateTimeOffset UpdatedAt { get; protected set; }

    protected void Touch(DateTimeOffset now)
    {
        UpdatedAt = now;
        Version++;
    }

    protected void RequireVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
        {
            throw new DomainException("concurrency_conflict", "The resource changed. Refresh and try again.");
        }
    }
}

