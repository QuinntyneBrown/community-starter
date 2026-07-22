using CommunityStarter.Application.Abstractions;
using CommunityStarter.Domain.Communities;
using CommunityStarter.Domain.Content;
using CommunityStarter.Domain.Identity;
using CommunityStarter.Domain.Moderation;
using CommunityStarter.Domain.Operations;
using Microsoft.EntityFrameworkCore;

namespace CommunityStarter.Infrastructure.Persistence;

public sealed class PlatformStore(CommunityDbContext dbContext) : IPlatformStore
{
    public ValueTask<Account?> FindAccountByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        new(dbContext.Accounts.SingleOrDefaultAsync(value => value.EmailNormalized == normalizedEmail, cancellationToken));

    public ValueTask<Account?> FindAccountAsync(Guid accountId, CancellationToken cancellationToken) =>
        dbContext.Accounts.FindAsync([accountId], cancellationToken);

    public ValueTask<ActionSecret?> FindActionSecretAsync(
        string purpose,
        string verifierHash,
        CancellationToken cancellationToken) =>
        new(dbContext.ActionSecrets.SingleOrDefaultAsync(
            value => value.Purpose == purpose && value.VerifierHash == verifierHash,
            cancellationToken));

    public ValueTask<AccountSession?> FindSessionAsync(string tokenHash, CancellationToken cancellationToken) =>
        new(dbContext.Sessions.SingleOrDefaultAsync(value => value.TokenHash == tokenHash, cancellationToken));

    public ValueTask<Community?> FindCommunityAsync(Guid communityId, CancellationToken cancellationToken) =>
        dbContext.Communities.FindAsync([communityId], cancellationToken);

    public ValueTask<Community?> FindCommunityBySlugAsync(string slug, CancellationToken cancellationToken) =>
        new(dbContext.Communities.SingleOrDefaultAsync(value => value.Slug == slug, cancellationToken));

    public ValueTask<Membership?> FindMembershipAsync(
        Guid communityId,
        Guid accountId,
        CancellationToken cancellationToken) =>
        new(dbContext.Memberships.SingleOrDefaultAsync(
            value => value.CommunityId == communityId && value.AccountId == accountId,
            cancellationToken));

    public ValueTask<CommunityInvitation?> FindInvitationAsync(
        string tokenHash,
        CancellationToken cancellationToken) =>
        new(dbContext.Invitations.SingleOrDefaultAsync(value => value.TokenHash == tokenHash, cancellationToken));

    public ValueTask<Post?> FindPostAsync(Guid postId, CancellationToken cancellationToken) =>
        dbContext.Posts.FindAsync([postId], cancellationToken);

    public ValueTask<Reaction?> FindReactionAsync(
        Guid postId,
        Guid accountId,
        string kind,
        CancellationToken cancellationToken) =>
        new(dbContext.Reactions.SingleOrDefaultAsync(
            value => value.PostId == postId && value.AccountId == accountId && value.Kind == kind,
            cancellationToken));

    public ValueTask<ModerationCase?> FindModerationCaseAsync(
        Guid caseId,
        CancellationToken cancellationToken) =>
        dbContext.ModerationCases.FindAsync([caseId], cancellationToken);

    public async ValueTask<IReadOnlyList<Post>> ListFeedAsync(
        Guid communityId,
        DateTimeOffset? before,
        Guid? beforeId,
        int take,
        CancellationToken cancellationToken)
    {
        IQueryable<Post> query = dbContext.Posts
            .AsNoTracking()
            .Where(value => value.CommunityId == communityId && value.Status == PostStatus.Published);
        if (before is not null && beforeId is not null)
        {
            query = query.Where(value =>
                value.PublishedAt < before || (value.PublishedAt == before && value.Id.CompareTo(beforeId.Value) < 0));
        }

        return await query
            .OrderByDescending(value => value.PublishedAt)
            .ThenByDescending(value => value.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async ValueTask<IReadOnlyList<Community>> ListPublicCommunitiesAsync(
        int take,
        CancellationToken cancellationToken) =>
        await dbContext.Communities
            .AsNoTracking()
            .Where(value => value.IsPubliclyListed)
            .OrderBy(value => value.Name)
            .ThenBy(value => value.Id)
            .Take(take)
            .ToListAsync(cancellationToken);

    public void AddAccount(Account account) => dbContext.Accounts.Add(account);
    public void AddActionSecret(ActionSecret actionSecret) => dbContext.ActionSecrets.Add(actionSecret);
    public void AddSession(AccountSession session) => dbContext.Sessions.Add(session);
    public void AddCommunity(Community community) => dbContext.Communities.Add(community);
    public void AddMembership(Membership membership) => dbContext.Memberships.Add(membership);
    public void AddInvitation(CommunityInvitation invitation) => dbContext.Invitations.Add(invitation);
    public void AddPost(Post post) => dbContext.Posts.Add(post);
    public void AddReaction(Reaction reaction) => dbContext.Reactions.Add(reaction);
    public void AddReport(Report report) => dbContext.Reports.Add(report);
    public void AddModerationCase(ModerationCase moderationCase) => dbContext.ModerationCases.Add(moderationCase);
    public void AddModerationAction(ModerationAction moderationAction) => dbContext.ModerationActions.Add(moderationAction);
    public void AddAuditEvent(AuditEvent auditEvent) => dbContext.AuditEvents.Add(auditEvent);
    public void AddOutboxMessage(OutboxMessage outboxMessage) => dbContext.OutboxMessages.Add(outboxMessage);
    public void AddJob(DurableJob job) => dbContext.Jobs.Add(job);

    public ValueTask<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        new(dbContext.SaveChangesAsync(cancellationToken));
}
