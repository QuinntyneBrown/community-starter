using System.Text.Json;
using CommunityStarter.Application.Abstractions;
using CommunityStarter.Application.Contracts;
using CommunityStarter.Domain.Common;
using CommunityStarter.Domain.Identity;
using CommunityStarter.Domain.Operations;

namespace CommunityStarter.Application.Services;

public sealed class IdentityService(
    IPlatformStore store,
    IPasswordService passwords,
    ISecretService secrets,
    IClock clock,
    ICorrelationContext correlation)
{
    private static readonly TimeSpan VerificationLifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(14);

    public async ValueTask<RegisterAccountResult> RegisterAsync(
        RegisterAccountCommand command,
        CancellationToken cancellationToken)
    {
        string normalized = Account.NormalizeEmail(command.Email);
        Account? existing = await store.FindAccountByEmailAsync(normalized, cancellationToken);
        if (existing is not null)
        {
            throw new DomainException("account_exists", "An account cannot be created with those details.");
        }

        DateTimeOffset now = clock.UtcNow;
        Account account = Account.Register(command.Email, "pending", now);
        account.ChangePassword(passwords.Hash(account, command.Password), now);
        string token = secrets.GenerateToken();
        ActionSecret action = ActionSecret.Issue(
            account.Id,
            "verify-account",
            secrets.HashToken(token),
            account.AuthenticationEpoch,
            now,
            VerificationLifetime);
        OutboxMessage outbox = OutboxMessage.Create(
            "account.verification-requested",
            JsonSerializer.Serialize(new { accountId = account.Id, actionId = action.Id }),
            now);

        store.AddAccount(account);
        store.AddActionSecret(action);
        store.AddOutboxMessage(outbox);
        store.AddAuditEvent(AuditEvent.Record(
            null,
            account.Id,
            "account.registered",
            "Account",
            account.Id,
            "{}",
            correlation.CorrelationId,
            now));
        await store.SaveChangesAsync(cancellationToken);
        return new(account.Id, account.Version, token);
    }

    public async ValueTask VerifyAsync(VerifyAccountCommand command, CancellationToken cancellationToken)
    {
        string verifier = secrets.HashToken(command.Token);
        ActionSecret? action = await store.FindActionSecretAsync(
            "verify-account",
            verifier,
            cancellationToken);
        DateTimeOffset now = clock.UtcNow;
        if (action is null || action.ConsumedAt is not null || action.ExpiresAt <= now)
        {
            throw new DomainException("action_secret_invalid", "The action is invalid or expired.");
        }

        Account account = await store.FindAccountAsync(action.AccountId, cancellationToken)
            ?? throw new DomainException("action_secret_invalid", "The action is invalid or expired.");
        if (action.AuthenticationEpoch != account.AuthenticationEpoch)
        {
            throw new DomainException("action_secret_invalid", "The action is invalid or expired.");
        }

        action.Consume(now);
        account.Verify(account.Version, now);
        store.AddAuditEvent(AuditEvent.Record(
            null,
            account.Id,
            "account.verified",
            "Account",
            account.Id,
            "{}",
            correlation.CorrelationId,
            now));
        await store.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask<SignInResult> SignInAsync(
        SignInCommand command,
        CancellationToken cancellationToken)
    {
        string normalized = Account.NormalizeEmail(command.Email);
        Account? account = await store.FindAccountByEmailAsync(normalized, cancellationToken);
        if (account is null ||
            account.Status != AccountStatus.Active ||
            !passwords.Verify(account, account.PasswordHash, command.Password))
        {
            throw new DomainException("sign_in_failed", "Email or password was not accepted.");
        }

        DateTimeOffset now = clock.UtcNow;
        string token = secrets.GenerateToken();
        AccountSession session = AccountSession.Start(
            account.Id,
            secrets.HashToken(token),
            account.AuthenticationEpoch,
            SanitizeDeviceLabel(command.DeviceLabel),
            now,
            SessionLifetime);
        store.AddSession(session);
        store.AddAuditEvent(AuditEvent.Record(
            null,
            account.Id,
            "session.started",
            "AccountSession",
            session.Id,
            "{}",
            correlation.CorrelationId,
            now));
        await store.SaveChangesAsync(cancellationToken);
        return new(account.Id, token, session.ExpiresAt);
    }

    public async ValueTask<CurrentAccount?> AuthenticateAsync(
        string? sessionToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return null;
        }

        AccountSession? session = await store.FindSessionAsync(
            secrets.HashToken(sessionToken),
            cancellationToken);
        if (session is null)
        {
            return null;
        }

        Account? account = await store.FindAccountAsync(session.AccountId, cancellationToken);
        if (account is null || account.Status != AccountStatus.Active || !session.IsActive(clock.UtcNow, account.AuthenticationEpoch))
        {
            return null;
        }

        return new(account.Id, account.EmailDisplay, account.Status, account.Locale, account.TimeZone);
    }

    public async ValueTask SignOutAsync(string? sessionToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return;
        }

        AccountSession? session = await store.FindSessionAsync(secrets.HashToken(sessionToken), cancellationToken);
        if (session is null)
        {
            return;
        }

        DateTimeOffset now = clock.UtcNow;
        session.Revoke(now);
        store.AddAuditEvent(AuditEvent.Record(
            null,
            session.AccountId,
            "session.revoked",
            "AccountSession",
            session.Id,
            "{}",
            correlation.CorrelationId,
            now));
        await store.SaveChangesAsync(cancellationToken);
    }

    private static string SanitizeDeviceLabel(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Unknown device";
        }

        string trimmed = value.Trim();
        return trimmed[..Math.Min(trimmed.Length, 120)];
    }
}
