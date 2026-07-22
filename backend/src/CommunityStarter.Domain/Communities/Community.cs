using CommunityStarter.Domain.Common;

namespace CommunityStarter.Domain.Communities;

public enum CommunityAccessMode
{
    Open,
    Gated,
    InvitationOnly
}

public enum MembershipStatus
{
    Active,
    Left,
    Removed,
    Suspended
}

public enum CommunityRole
{
    Member,
    Moderator,
    Administrator,
    Owner
}

public sealed class Community : Entity
{
    private Community() { }

    public string Slug { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public CommunityAccessMode AccessMode { get; private set; }
    public bool IsPubliclyListed { get; private set; }

    public static Community Create(string slug, string name, string description, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        Slug = NormalizeSlug(slug),
        Name = RequireText(name, nameof(name), 120),
        Description = RequireText(description, nameof(description), 2_000),
        AccessMode = CommunityAccessMode.Gated,
        CreatedAt = now,
        UpdatedAt = now
    };

    public void Configure(
        string name,
        string description,
        CommunityAccessMode accessMode,
        bool isPubliclyListed,
        long expectedVersion,
        DateTimeOffset now)
    {
        RequireVersion(expectedVersion);
        Name = RequireText(name, nameof(name), 120);
        Description = RequireText(description, nameof(description), 2_000);
        AccessMode = accessMode;
        IsPubliclyListed = isPubliclyListed;
        Touch(now);
    }

    public static string NormalizeSlug(string slug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        var value = slug.Trim().ToLowerInvariant();
        if (value.Length is < 3 or > 64 || value.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
        {
            throw new DomainException("invalid_community_slug", "Use 3–64 lowercase letters, numbers, or hyphens.");
        }

        return value;
    }

    private static string RequireText(string value, string name, int maximum)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        var trimmed = value.Trim();
        if (trimmed.Length > maximum)
        {
            throw new DomainException("value_too_long", $"{name} is too long.");
        }

        return trimmed;
    }
}

public sealed class Membership : Entity
{
    private Membership() { }

    public Guid CommunityId { get; private init; }
    public Guid AccountId { get; private init; }
    public MembershipStatus Status { get; private set; }
    public CommunityRole Role { get; private set; }

    public static Membership CreateOwner(Guid communityId, Guid accountId, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        CommunityId = communityId,
        AccountId = accountId,
        Status = MembershipStatus.Active,
        Role = CommunityRole.Owner,
        CreatedAt = now,
        UpdatedAt = now
    };

    public static Membership Join(Guid communityId, Guid accountId, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        CommunityId = communityId,
        AccountId = accountId,
        Status = MembershipStatus.Active,
        Role = CommunityRole.Member,
        CreatedAt = now,
        UpdatedAt = now
    };

    public bool Can(string permission) => Status == MembershipStatus.Active && permission switch
    {
        "community.manage" => Role is CommunityRole.Owner or CommunityRole.Administrator,
        "member.invite" => Role is CommunityRole.Owner or CommunityRole.Administrator,
        "content.publish" => true,
        "content.moderate" => Role is CommunityRole.Owner or CommunityRole.Administrator or CommunityRole.Moderator,
        _ => false
    };

    public void AssignRole(CommunityRole role, long expectedVersion, DateTimeOffset now)
    {
        RequireVersion(expectedVersion);
        if (Status != MembershipStatus.Active)
        {
            throw new DomainException("membership_not_active", "An inactive membership cannot receive a role.");
        }

        Role = role;
        Touch(now);
    }
}

public sealed class CommunityInvitation : Entity
{
    private CommunityInvitation() { }

    public Guid CommunityId { get; private init; }
    public Guid InvitedByAccountId { get; private init; }
    public string EmailNormalized { get; private init; } = string.Empty;
    public string TokenHash { get; private init; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private init; }
    public DateTimeOffset? AcceptedAt { get; private set; }

    public static CommunityInvitation Issue(
        Guid communityId,
        Guid invitedByAccountId,
        string email,
        string tokenHash,
        DateTimeOffset now,
        TimeSpan lifetime) => new()
        {
            Id = Guid.CreateVersion7(),
            CommunityId = communityId,
            InvitedByAccountId = invitedByAccountId,
            EmailNormalized = Identity.Account.NormalizeEmail(email),
            TokenHash = tokenHash,
            ExpiresAt = now.Add(lifetime),
            CreatedAt = now,
            UpdatedAt = now
        };

    public void Accept(DateTimeOffset now)
    {
        if (AcceptedAt is not null || ExpiresAt <= now)
        {
            throw new DomainException("invitation_invalid", "The invitation is invalid or expired.");
        }

        AcceptedAt = now;
        Touch(now);
    }
}

