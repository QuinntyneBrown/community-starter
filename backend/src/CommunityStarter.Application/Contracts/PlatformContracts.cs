using CommunityStarter.Domain.Communities;
using CommunityStarter.Domain.Identity;

namespace CommunityStarter.Application.Contracts;

public sealed record RegisterAccountCommand(string Email, string Password);
public sealed record RegisterAccountResult(Guid AccountId, long Version, string VerificationToken);
public sealed record VerifyAccountCommand(string Token);
public sealed record SignInCommand(string Email, string Password, string DeviceLabel);
public sealed record SignInResult(Guid AccountId, string SessionToken, DateTimeOffset ExpiresAt);
public sealed record CurrentAccount(Guid Id, string Email, AccountStatus Status, string Locale, string TimeZone);

public sealed record CreateCommunityCommand(string Slug, string Name, string Description);
public sealed record ConfigureCommunityCommand(
    string Name,
    string Description,
    CommunityAccessMode AccessMode,
    bool IsPubliclyListed,
    long ExpectedVersion);
public sealed record CommunityResult(
    Guid Id,
    string Slug,
    string Name,
    string Description,
    CommunityAccessMode AccessMode,
    bool IsPubliclyListed,
    long Version);
public sealed record InviteMemberCommand(string Email);
public sealed record InvitationResult(Guid InvitationId, string Token, DateTimeOffset ExpiresAt);
public sealed record AcceptInvitationCommand(string Token);

public sealed record PublishPostCommand(string Body);
public sealed record EditPostCommand(string Body, long ExpectedVersion);
public sealed record PostResult(
    Guid Id,
    Guid CommunityId,
    Guid AuthorAccountId,
    string Body,
    string Status,
    DateTimeOffset? PublishedAt,
    long Version,
    int ReactionCount = 0);
public sealed record AddReactionCommand(string Kind);
public sealed record SubmitReportCommand(string Reason);
public sealed record ReportResult(Guid ReportId, Guid CaseId);
public sealed record ApplyModerationActionCommand(
    Guid PostId,
    string Kind,
    string Rationale,
    long ExpectedCaseVersion,
    long ExpectedPostVersion);
public sealed record ModerationActionResult(Guid ActionId, long PostVersion);

public sealed record CursorPage<T>(IReadOnlyList<T> Items, string? NextCursor);

