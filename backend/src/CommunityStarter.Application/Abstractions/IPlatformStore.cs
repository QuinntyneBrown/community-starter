using CommunityStarter.Domain.Communities;
using CommunityStarter.Domain.Content;
using CommunityStarter.Domain.Identity;
using CommunityStarter.Domain.Moderation;
using CommunityStarter.Domain.Operations;

namespace CommunityStarter.Application.Abstractions;

public interface IPlatformStore
{
    ValueTask<Account?> FindAccountByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    ValueTask<Account?> FindAccountAsync(Guid accountId, CancellationToken cancellationToken);
    ValueTask<ActionSecret?> FindActionSecretAsync(
        string purpose,
        string verifierHash,
        CancellationToken cancellationToken);
    ValueTask<AccountSession?> FindSessionAsync(string tokenHash, CancellationToken cancellationToken);
    ValueTask<Community?> FindCommunityAsync(Guid communityId, CancellationToken cancellationToken);
    ValueTask<Community?> FindCommunityBySlugAsync(string slug, CancellationToken cancellationToken);
    ValueTask<Membership?> FindMembershipAsync(
        Guid communityId,
        Guid accountId,
        CancellationToken cancellationToken);
    ValueTask<CommunityInvitation?> FindInvitationAsync(
        string tokenHash,
        CancellationToken cancellationToken);
    ValueTask<Post?> FindPostAsync(Guid postId, CancellationToken cancellationToken);
    ValueTask<Reaction?> FindReactionAsync(
        Guid postId,
        Guid accountId,
        string kind,
        CancellationToken cancellationToken);
    ValueTask<ModerationCase?> FindModerationCaseAsync(
        Guid caseId,
        CancellationToken cancellationToken);
    ValueTask<IReadOnlyList<Post>> ListFeedAsync(
        Guid communityId,
        DateTimeOffset? before,
        Guid? beforeId,
        int take,
        CancellationToken cancellationToken);
    ValueTask<IReadOnlyList<Community>> ListPublicCommunitiesAsync(
        int take,
        CancellationToken cancellationToken);

    void AddAccount(Account account);
    void AddActionSecret(ActionSecret actionSecret);
    void AddSession(AccountSession session);
    void AddCommunity(Community community);
    void AddMembership(Membership membership);
    void AddInvitation(CommunityInvitation invitation);
    void AddPost(Post post);
    void AddReaction(Reaction reaction);
    void AddReport(Report report);
    void AddModerationCase(ModerationCase moderationCase);
    void AddModerationAction(ModerationAction moderationAction);
    void AddAuditEvent(AuditEvent auditEvent);
    void AddOutboxMessage(OutboxMessage outboxMessage);
    void AddJob(DurableJob job);

    ValueTask<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IPasswordService
{
    string Hash(Account account, string password);
    bool Verify(Account account, string hash, string password);
}

public interface ISecretService
{
    string GenerateToken();
    string HashToken(string token);
}

public interface ICorrelationContext
{
    string CorrelationId { get; }
}

