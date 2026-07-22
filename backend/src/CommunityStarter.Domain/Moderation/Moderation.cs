using CommunityStarter.Domain.Common;

namespace CommunityStarter.Domain.Moderation;

public enum ModerationCaseStatus
{
    Open,
    InReview,
    Resolved,
    Appealed
}

public sealed class Report : Entity
{
    private Report() { }

    public Guid CommunityId { get; private init; }
    public Guid ReporterAccountId { get; private init; }
    public string TargetType { get; private init; } = string.Empty;
    public Guid TargetId { get; private init; }
    public string Reason { get; private init; } = string.Empty;

    public static Report Submit(
        Guid communityId,
        Guid reporterAccountId,
        string targetType,
        Guid targetId,
        string reason,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        return new Report
        {
            Id = Guid.CreateVersion7(),
            CommunityId = communityId,
            ReporterAccountId = reporterAccountId,
            TargetType = targetType,
            TargetId = targetId,
            Reason = reason.Trim()[..Math.Min(reason.Trim().Length, 2_000)],
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

public sealed class ModerationCase : Entity
{
    private ModerationCase() { }

    public Guid CommunityId { get; private init; }
    public Guid ReportId { get; private init; }
    public ModerationCaseStatus Status { get; private set; }
    public Guid? AssignedAccountId { get; private set; }

    public static ModerationCase Open(Guid communityId, Guid reportId, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        CommunityId = communityId,
        ReportId = reportId,
        Status = ModerationCaseStatus.Open,
        CreatedAt = now,
        UpdatedAt = now
    };

    public void Resolve(Guid moderatorAccountId, long expectedVersion, DateTimeOffset now)
    {
        RequireVersion(expectedVersion);
        if (Status == ModerationCaseStatus.Resolved)
        {
            throw new DomainException("case_already_resolved", "The case is already resolved.");
        }

        AssignedAccountId = moderatorAccountId;
        Status = ModerationCaseStatus.Resolved;
        Touch(now);
    }
}

public sealed class ModerationAction : Entity
{
    private ModerationAction() { }

    public Guid CommunityId { get; private init; }
    public Guid CaseId { get; private init; }
    public Guid ModeratorAccountId { get; private init; }
    public string TargetType { get; private init; } = string.Empty;
    public Guid TargetId { get; private init; }
    public string Kind { get; private init; } = string.Empty;
    public string Rationale { get; private init; } = string.Empty;

    public static ModerationAction Apply(
        Guid communityId,
        Guid caseId,
        Guid moderatorAccountId,
        string targetType,
        Guid targetId,
        string kind,
        string rationale,
        DateTimeOffset now) => new()
        {
            Id = Guid.CreateVersion7(),
            CommunityId = communityId,
            CaseId = caseId,
            ModeratorAccountId = moderatorAccountId,
            TargetType = targetType,
            TargetId = targetId,
            Kind = kind,
            Rationale = rationale.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
}

