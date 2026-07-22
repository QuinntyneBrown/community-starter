using System.Globalization;
using System.Text;
using System.Text.Json;
using CommunityStarter.Application.Abstractions;
using CommunityStarter.Application.Contracts;
using CommunityStarter.Domain.Common;
using CommunityStarter.Domain.Communities;
using CommunityStarter.Domain.Content;
using CommunityStarter.Domain.Moderation;
using CommunityStarter.Domain.Operations;

namespace CommunityStarter.Application.Services;

public sealed class ContentService(
    IPlatformStore store,
    IClock clock,
    ICorrelationContext correlation)
{
    public async ValueTask<PostResult> PublishAsync(
        Guid accountId,
        Guid communityId,
        PublishPostCommand command,
        CancellationToken cancellationToken)
    {
        Membership membership = await RequireMemberAsync(communityId, accountId, cancellationToken);
        if (!membership.Can("content.publish"))
        {
            throw new DomainException("permission_denied", "The account cannot publish in this community.");
        }

        DateTimeOffset now = clock.UtcNow;
        Post post = Post.Publish(communityId, accountId, command.Body, now);
        store.AddPost(post);
        store.AddOutboxMessage(OutboxMessage.Create(
            "post.published",
            JsonSerializer.Serialize(new { postId = post.Id, communityId }),
            now));
        await store.SaveChangesAsync(cancellationToken);
        return ToResult(post);
    }

    public async ValueTask<PostResult> EditAsync(
        Guid accountId,
        Guid communityId,
        Guid postId,
        EditPostCommand command,
        CancellationToken cancellationToken)
    {
        Membership membership = await RequireMemberAsync(communityId, accountId, cancellationToken);
        Post post = await RequireVisiblePostAsync(communityId, postId, cancellationToken);
        post.Edit(accountId, command.Body, command.ExpectedVersion, membership.Can("content.moderate"), clock.UtcNow);
        await store.SaveChangesAsync(cancellationToken);
        return ToResult(post);
    }

    public async ValueTask<CursorPage<PostResult>> FeedAsync(
        Guid accountId,
        Guid communityId,
        string? cursor,
        int take,
        CancellationToken cancellationToken)
    {
        _ = await RequireMemberAsync(communityId, accountId, cancellationToken);
        (DateTimeOffset? before, Guid? beforeId) = DecodeCursor(cursor);
        IReadOnlyList<Post> posts = await store.ListFeedAsync(
            communityId,
            before,
            beforeId,
            Math.Clamp(take, 1, 50) + 1,
            cancellationToken);
        bool hasMore = posts.Count > Math.Clamp(take, 1, 50);
        Post[] page = posts.Take(Math.Clamp(take, 1, 50)).ToArray();
        string? next = hasMore && page.Length > 0 ? EncodeCursor(page[^1]) : null;
        return new(page.Select(ToResult).ToArray(), next);
    }

    public async ValueTask AddReactionAsync(
        Guid accountId,
        Guid communityId,
        Guid postId,
        AddReactionCommand command,
        CancellationToken cancellationToken)
    {
        _ = await RequireMemberAsync(communityId, accountId, cancellationToken);
        _ = await RequireVisiblePostAsync(communityId, postId, cancellationToken);
        Reaction? existing = await store.FindReactionAsync(postId, accountId, command.Kind, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        store.AddReaction(Reaction.Add(communityId, postId, accountId, command.Kind, clock.UtcNow));
        await store.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask<ReportResult> ReportAsync(
        Guid accountId,
        Guid communityId,
        Guid postId,
        SubmitReportCommand command,
        CancellationToken cancellationToken)
    {
        _ = await RequireMemberAsync(communityId, accountId, cancellationToken);
        _ = await RequireVisiblePostAsync(communityId, postId, cancellationToken);
        DateTimeOffset now = clock.UtcNow;
        Report report = Report.Submit(communityId, accountId, "Post", postId, command.Reason, now);
        ModerationCase moderationCase = ModerationCase.Open(communityId, report.Id, now);
        store.AddReport(report);
        store.AddModerationCase(moderationCase);
        store.AddAuditEvent(AuditEvent.Record(
            communityId,
            accountId,
            "report.submitted",
            "Post",
            postId,
            "{}",
            correlation.CorrelationId,
            now));
        await store.SaveChangesAsync(cancellationToken);
        return new(report.Id, moderationCase.Id);
    }

    private async ValueTask<Membership> RequireMemberAsync(
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

    private async ValueTask<Post> RequireVisiblePostAsync(
        Guid communityId,
        Guid postId,
        CancellationToken cancellationToken)
    {
        Post? post = await store.FindPostAsync(postId, cancellationToken);
        if (post is null || post.CommunityId != communityId || post.Status != PostStatus.Published)
        {
            throw new DomainException("post_not_found", "The post was not found.");
        }

        return post;
    }

    private static PostResult ToResult(Post post) => new(
        post.Id,
        post.CommunityId,
        post.AuthorAccountId,
        post.Body,
        post.Status.ToString(),
        post.PublishedAt,
        post.Version);

    private static string EncodeCursor(Post post)
    {
        string value = string.Create(
            CultureInfo.InvariantCulture,
            $"{post.PublishedAt:O}|{post.Id:D}");
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static (DateTimeOffset? Before, Guid? BeforeId) DecodeCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return (null, null);
        }

        try
        {
            string padded = cursor.Replace('-', '+').Replace('_', '/');
            padded = padded.PadRight(padded.Length + ((4 - (padded.Length % 4)) % 4), '=');
            string[] parts = Encoding.UTF8.GetString(Convert.FromBase64String(padded)).Split('|');
            return parts.Length == 2 &&
                DateTimeOffset.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset before) &&
                Guid.TryParse(parts[1], out Guid id)
                ? (before, id)
                : throw new FormatException();
        }
        catch (FormatException)
        {
            throw new DomainException("cursor_invalid", "The page cursor is invalid.");
        }
    }
}

