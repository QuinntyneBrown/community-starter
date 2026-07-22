using System.Text.Json;
using CommunityStarter.Application.Abstractions;
using CommunityStarter.Application.Contracts;
using CommunityStarter.Domain.Common;
using CommunityStarter.Domain.Communities;
using CommunityStarter.Domain.Content;
using CommunityStarter.Domain.Moderation;
using CommunityStarter.Domain.Operations;

namespace CommunityStarter.Application.Services;

public sealed class ModerationService(
    IPlatformStore store,
    IClock clock,
    ICorrelationContext correlation)
{
    public async ValueTask<ModerationActionResult> ApplyActionAsync(
        Guid moderatorAccountId,
        Guid communityId,
        Guid caseId,
        ApplyModerationActionCommand command,
        CancellationToken cancellationToken)
    {
        Membership? membership = await store.FindMembershipAsync(
            communityId,
            moderatorAccountId,
            cancellationToken);
        if (membership is null || !membership.Can("content.moderate"))
        {
            throw new DomainException("permission_denied", "The account cannot moderate this community.");
        }

        ModerationCase moderationCase = await store.FindModerationCaseAsync(caseId, cancellationToken)
            ?? throw new DomainException("moderation_case_not_found", "The moderation case was not found.");
        Post post = await store.FindPostAsync(command.PostId, cancellationToken)
            ?? throw new DomainException("post_not_found", "The post was not found.");
        if (moderationCase.CommunityId != communityId || post.CommunityId != communityId)
        {
            throw new DomainException("moderation_case_not_found", "The moderation case was not found.");
        }

        DateTimeOffset now = clock.UtcNow;
        moderationCase.Resolve(moderatorAccountId, command.ExpectedCaseVersion, now);
        post.Moderate(command.ExpectedPostVersion, now);
        ModerationAction action = ModerationAction.Apply(
            communityId,
            caseId,
            moderatorAccountId,
            "Post",
            post.Id,
            command.Kind,
            command.Rationale,
            now);
        store.AddModerationAction(action);
        store.AddAuditEvent(AuditEvent.Record(
            communityId,
            moderatorAccountId,
            "moderation.action-applied",
            "Post",
            post.Id,
            JsonSerializer.Serialize(new { action.Id, action.Kind, caseId }),
            correlation.CorrelationId,
            now));
        store.AddOutboxMessage(OutboxMessage.Create(
            "moderation.action-applied",
            JsonSerializer.Serialize(new { action.Id, postId = post.Id, communityId, post.Version }),
            now));
        store.AddJob(DurableJob.Enqueue(
            "moderation.reconcile",
            JsonSerializer.Serialize(new { action.Id, postId = post.Id, communityId }),
            now));
        await store.SaveChangesAsync(cancellationToken);
        return new(action.Id, post.Version);
    }
}

