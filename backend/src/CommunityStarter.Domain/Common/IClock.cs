namespace CommunityStarter.Domain.Common;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

