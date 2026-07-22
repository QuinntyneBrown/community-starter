using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using CommunityStarter.Application.Abstractions;
using CommunityStarter.Domain.Common;
using CommunityStarter.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CommunityStarter.Infrastructure.Security;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";
    public string TokenPepper { get; init; } = string.Empty;
}

public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<Account> hasher = new();

    public string Hash(Account account, string password)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        if (password.Length < 12 || password.Length > 256)
        {
            throw new DomainException("password_invalid", "Use a password between 12 and 256 characters.");
        }

        return hasher.HashPassword(account, password);
    }

    public bool Verify(Account account, string hash, string password)
    {
        ArgumentNullException.ThrowIfNull(account);
        if (string.IsNullOrEmpty(password))
        {
            return false;
        }

        PasswordVerificationResult result = hasher.VerifyHashedPassword(account, hash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}

public sealed class SecretService : ISecretService
{
    private readonly byte[] pepper;

    public SecretService(IOptions<SecurityOptions> options)
    {
        string configured = options.Value.TokenPepper;
        if (string.IsNullOrWhiteSpace(configured) || configured.Length < 32)
        {
            throw new InvalidOperationException("Security:TokenPepper must contain at least 32 characters.");
        }

        pepper = Encoding.UTF8.GetBytes(configured);
    }

    public string GenerateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');

    public string HashToken(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return Convert.ToHexString(HMACSHA256.HashData(pepper, Encoding.UTF8.GetBytes(token)));
    }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public sealed class CorrelationContext : ICorrelationContext
{
    public string CorrelationId => Activity.Current?.TraceId.ToString() ?? "background";
}

