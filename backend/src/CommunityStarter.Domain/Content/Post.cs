using CommunityStarter.Domain.Common;

namespace CommunityStarter.Domain.Content;

public enum PostStatus
{
    Draft,
    Published,
    Removed,
    Moderated
}

public sealed class Post : Entity
{
    private Post() { }

    public Guid CommunityId { get; private init; }
    public Guid AuthorAccountId { get; private init; }
    public string Body { get; private set; } = string.Empty;
    public PostStatus Status { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }

    public static Post Publish(Guid communityId, Guid authorAccountId, string body, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        CommunityId = communityId,
        AuthorAccountId = authorAccountId,
        Body = ValidateBody(body),
        Status = PostStatus.Published,
        PublishedAt = now,
        CreatedAt = now,
        UpdatedAt = now
    };

    public void Edit(Guid actorAccountId, string body, long expectedVersion, bool canModerate, DateTimeOffset now)
    {
        RequireVersion(expectedVersion);
        if (actorAccountId != AuthorAccountId && !canModerate)
        {
            throw new DomainException("permission_denied", "The account cannot edit this post.");
        }

        if (Status is PostStatus.Removed or PostStatus.Moderated)
        {
            throw new DomainException("post_not_editable", "The post cannot be edited in its current state.");
        }

        Body = ValidateBody(body);
        Touch(now);
    }

    public void Moderate(long expectedVersion, DateTimeOffset now)
    {
        RequireVersion(expectedVersion);
        if (Status != PostStatus.Published)
        {
            throw new DomainException("post_not_moderatable", "The post is not currently published.");
        }

        Status = PostStatus.Moderated;
        Touch(now);
    }

    private static string ValidateBody(string body)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        var value = body.Trim();
        if (value.Length > 20_000)
        {
            throw new DomainException("post_too_long", "The post exceeds the allowed length.");
        }

        return value;
    }
}

public sealed class Reaction : Entity
{
    private Reaction() { }

    public Guid CommunityId { get; private init; }
    public Guid PostId { get; private init; }
    public Guid AccountId { get; private init; }
    public string Kind { get; private init; } = string.Empty;

    public static Reaction Add(Guid communityId, Guid postId, Guid accountId, string kind, DateTimeOffset now)
    {
        var allowed = new[] { "appreciate", "support", "insightful" };
        if (!allowed.Contains(kind, StringComparer.Ordinal))
        {
            throw new DomainException("reaction_invalid", "The reaction is not supported.");
        }

        return new Reaction
        {
            Id = Guid.CreateVersion7(),
            CommunityId = communityId,
            PostId = postId,
            AccountId = accountId,
            Kind = kind,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

