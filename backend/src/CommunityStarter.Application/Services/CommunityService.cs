using System.Text.Json;
using CommunityStarter.Application.Abstractions;
using CommunityStarter.Application.Contracts;
using CommunityStarter.Domain.Common;
using CommunityStarter.Domain.Communities;
using CommunityStarter.Domain.Identity;
using CommunityStarter.Domain.Operations;

namespace CommunityStarter.Application.Services;

public sealed class CommunityService(
    IPlatformStore store,
    ISecretService secrets,
    IClock clock,
    ICorrelationContext correlation)
{
    public async ValueTask<CommunityResult> CreateAsync(
        Guid accountId,
        CreateCommunityCommand command,
        CancellationToken cancellationToken)
    {
        string slug = Community.NormalizeSlug(command.Slug);
        if (await store.FindCommunityBySlugAsync(slug, cancellationToken) is not null)
        {
            throw new DomainException("community_slug_unavailable", "That community address is unavailable.");
        }

        DateTimeOffset now = clock.UtcNow;
        Community community = Community.Create(slug, command.Name, command.Description, now);
        Membership owner = Membership.CreateOwner(community.Id, accountId, now);
        store.AddCommunity(community);
        store.AddMembership(owner);
        store.AddAuditEvent(AuditEvent.Record(
            community.Id,
            accountId,
            "community.created",
            "Community",
            community.Id,
            "{}",
            correlation.CorrelationId,
            now));
        store.AddOutboxMessage(OutboxMessage.Create(
            "community.created",
            JsonSerializer.Serialize(new { communityId = community.Id }),
            now));
        await store.SaveChangesAsync(cancellationToken);
        return ToResult(community);
    }

    public async ValueTask<CommunityResult> ConfigureAsync(
        Guid accountId,
        Guid communityId,
        ConfigureCommunityCommand command,
        CancellationToken cancellationToken)
    {
        Community community = await store.FindCommunityAsync(communityId, cancellationToken)
            ?? throw new DomainException("community_not_found", "The community was not found.");
        Membership membership = await RequireMembershipAsync(communityId, accountId, cancellationToken);
        if (!membership.Can("community.manage"))
        {
            throw new DomainException("permission_denied", "The account cannot manage this community.");
        }

        DateTimeOffset now = clock.UtcNow;
        community.Configure(
            command.Name,
            command.Description,
            command.AccessMode,
            command.IsPubliclyListed,
            command.ExpectedVersion,
            now);
        store.AddAuditEvent(AuditEvent.Record(
            community.Id,
            accountId,
            "community.configured",
            "Community",
            community.Id,
            JsonSerializer.Serialize(new { command.AccessMode, command.IsPubliclyListed }),
            correlation.CorrelationId,
            now));
        await store.SaveChangesAsync(cancellationToken);
        return ToResult(community);
    }

    public async ValueTask<InvitationResult> InviteAsync(
        Guid accountId,
        Guid communityId,
        InviteMemberCommand command,
        CancellationToken cancellationToken)
    {
        _ = await store.FindCommunityAsync(communityId, cancellationToken)
            ?? throw new DomainException("community_not_found", "The community was not found.");
        Membership membership = await RequireMembershipAsync(communityId, accountId, cancellationToken);
        if (!membership.Can("member.invite"))
        {
            throw new DomainException("permission_denied", "The account cannot invite members.");
        }

        DateTimeOffset now = clock.UtcNow;
        string token = secrets.GenerateToken();
        CommunityInvitation invitation = CommunityInvitation.Issue(
            communityId,
            accountId,
            command.Email,
            secrets.HashToken(token),
            now,
            TimeSpan.FromDays(7));
        store.AddInvitation(invitation);
        store.AddOutboxMessage(OutboxMessage.Create(
            "community.invitation-issued",
            JsonSerializer.Serialize(new { invitationId = invitation.Id, communityId }),
            now));
        await store.SaveChangesAsync(cancellationToken);
        return new(invitation.Id, token, invitation.ExpiresAt);
    }

    public async ValueTask AcceptInvitationAsync(
        Guid accountId,
        AcceptInvitationCommand command,
        CancellationToken cancellationToken)
    {
        CommunityInvitation invitation = await store.FindInvitationAsync(
            secrets.HashToken(command.Token),
            cancellationToken) ?? throw new DomainException("invitation_invalid", "The invitation is invalid or expired.");
        Account account = await store.FindAccountAsync(accountId, cancellationToken)
            ?? throw new DomainException("account_not_found", "The account was not found.");
        if (!string.Equals(account.EmailNormalized, invitation.EmailNormalized, StringComparison.Ordinal))
        {
            throw new DomainException("invitation_invalid", "The invitation is invalid or expired.");
        }

        Membership? existing = await store.FindMembershipAsync(invitation.CommunityId, accountId, cancellationToken);
        if (existing is not null)
        {
            throw new DomainException("membership_exists", "The account already has a membership identity.");
        }

        DateTimeOffset now = clock.UtcNow;
        invitation.Accept(now);
        store.AddMembership(Membership.Join(invitation.CommunityId, accountId, now));
        store.AddOutboxMessage(OutboxMessage.Create(
            "membership.joined",
            JsonSerializer.Serialize(new { invitation.CommunityId, accountId }),
            now));
        await store.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask<IReadOnlyList<CommunityResult>> ListPublicAsync(
        int take,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Community> communities = await store.ListPublicCommunitiesAsync(
            Math.Clamp(take, 1, 50),
            cancellationToken);
        return communities.Select(ToResult).ToArray();
    }

    private async ValueTask<Membership> RequireMembershipAsync(
        Guid communityId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        Membership? membership = await store.FindMembershipAsync(communityId, accountId, cancellationToken);
        if (membership is null || membership.Status != MembershipStatus.Active)
        {
            throw new DomainException("community_not_found", "The community was not found.");
        }

        return membership;
    }

    private static CommunityResult ToResult(Community community) => new(
        community.Id,
        community.Slug,
        community.Name,
        community.Description,
        community.AccessMode,
        community.IsPubliclyListed,
        community.Version);
}
