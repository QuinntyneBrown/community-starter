using CommunityStarter.Domain.Common;

namespace CommunityStarter.Domain.Identity;

public enum AccountStatus
{
    PendingVerification,
    Active,
    Deactivated,
    DeletionPending,
    Deleted
}

public sealed class Account : Entity
{
    private Account() { }

    public string EmailNormalized { get; private set; } = string.Empty;
    public string EmailDisplay { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public AccountStatus Status { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }
    public long AuthenticationEpoch { get; private set; }
    public string Locale { get; private set; } = "en-CA";
    public string TimeZone { get; private set; } = "UTC";

    public static Account Register(string email, string passwordHash, DateTimeOffset now)
    {
        var normalized = NormalizeEmail(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new Account
        {
            Id = Guid.CreateVersion7(),
            EmailNormalized = normalized,
            EmailDisplay = email.Trim(),
            PasswordHash = passwordHash,
            Status = AccountStatus.PendingVerification,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Verify(long expectedVersion, DateTimeOffset now)
    {
        RequireVersion(expectedVersion);
        if (Status != AccountStatus.PendingVerification)
        {
            throw new DomainException("account_not_pending", "The account cannot be verified in its current state.");
        }

        Status = AccountStatus.Active;
        VerifiedAt = now;
        Touch(now);
    }

    public void ChangePassword(string passwordHash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
        AuthenticationEpoch++;
        Touch(now);
    }

    public void Deactivate(long expectedVersion, DateTimeOffset now)
    {
        RequireVersion(expectedVersion);
        if (Status != AccountStatus.Active)
        {
            throw new DomainException("account_not_active", "Only an active account can be deactivated.");
        }

        Status = AccountStatus.Deactivated;
        AuthenticationEpoch++;
        Touch(now);
    }

    public static string NormalizeEmail(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return email.Trim().Normalize().ToUpperInvariant();
    }
}

public sealed class ActionSecret : Entity
{
    private ActionSecret() { }

    public Guid AccountId { get; private init; }
    public string Purpose { get; private init; } = string.Empty;
    public string VerifierHash { get; private init; } = string.Empty;
    public long AuthenticationEpoch { get; private init; }
    public DateTimeOffset ExpiresAt { get; private init; }
    public DateTimeOffset? ConsumedAt { get; private set; }

    public static ActionSecret Issue(
        Guid accountId,
        string purpose,
        string verifierHash,
        long authenticationEpoch,
        DateTimeOffset now,
        TimeSpan lifetime) => new()
        {
            Id = Guid.CreateVersion7(),
            AccountId = accountId,
            Purpose = purpose,
            VerifierHash = verifierHash,
            AuthenticationEpoch = authenticationEpoch,
            ExpiresAt = now.Add(lifetime),
            CreatedAt = now,
            UpdatedAt = now
        };

    public void Consume(DateTimeOffset now)
    {
        if (ConsumedAt is not null || ExpiresAt <= now)
        {
            throw new DomainException("action_secret_invalid", "The action is invalid or expired.");
        }

        ConsumedAt = now;
        Touch(now);
    }
}

public sealed class AccountSession : Entity
{
    private AccountSession() { }

    public Guid AccountId { get; private init; }
    public Guid FamilyId { get; private init; }
    public string TokenHash { get; private set; } = string.Empty;
    public long AuthenticationEpoch { get; private init; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string DeviceLabel { get; private init; } = string.Empty;

    public bool IsActive(DateTimeOffset now, long currentEpoch) =>
        RevokedAt is null && ExpiresAt > now && AuthenticationEpoch == currentEpoch;

    public static AccountSession Start(
        Guid accountId,
        string tokenHash,
        long authenticationEpoch,
        string deviceLabel,
        DateTimeOffset now,
        TimeSpan lifetime) => new()
        {
            Id = Guid.CreateVersion7(),
            FamilyId = Guid.CreateVersion7(),
            AccountId = accountId,
            TokenHash = tokenHash,
            AuthenticationEpoch = authenticationEpoch,
            DeviceLabel = deviceLabel,
            ExpiresAt = now.Add(lifetime),
            CreatedAt = now,
            UpdatedAt = now
        };

    public void Rotate(string replacementHash, DateTimeOffset now, TimeSpan lifetime)
    {
        if (RevokedAt is not null || ExpiresAt <= now)
        {
            throw new DomainException("session_expired", "The session cannot be renewed.");
        }

        TokenHash = replacementHash;
        ExpiresAt = now.Add(lifetime);
        Touch(now);
    }

    public void Revoke(DateTimeOffset now)
    {
        if (RevokedAt is null)
        {
            RevokedAt = now;
            Touch(now);
        }
    }
}

