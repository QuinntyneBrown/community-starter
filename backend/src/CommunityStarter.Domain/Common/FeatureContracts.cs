namespace CommunityStarter.Domain.Common;

public enum FeatureDecisionKind
{
    Accepted,
    Invalid,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict
}

public sealed record FeatureInput(
    Guid SubjectId,
    string Action,
    long ExpectedVersion,
    Guid? CommunityId,
    IReadOnlyDictionary<string, string> Values);

public sealed record FeatureState(
    Guid Id,
    Guid? CommunityId,
    string State,
    long Version,
    bool IsAuthorized,
    bool IsAvailable);

public sealed record FeatureDecision(
    string RequirementId,
    FeatureDecisionKind Kind,
    string Code,
    string ResultingState,
    long ResultingVersion)
{
    public bool IsAccepted => Kind == FeatureDecisionKind.Accepted;
}

public static class FeaturePolicyEvaluator
{
    public static FeatureDecision Evaluate(string requirementId, FeatureState record, FeatureInput request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requirementId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Action);

        if (!record.IsAvailable)
        {
            return new(requirementId, FeatureDecisionKind.NotFound, "resource_not_found", record.State, record.Version);
        }

        if (!record.IsAuthorized)
        {
            return new(requirementId, FeatureDecisionKind.Forbidden, "permission_denied", record.State, record.Version);
        }

        if (record.Version != request.ExpectedVersion)
        {
            return new(requirementId, FeatureDecisionKind.Conflict, "concurrency_conflict", record.State, record.Version);
        }

        return new(requirementId, FeatureDecisionKind.Accepted, "accepted", request.Action, record.Version + 1);
    }
}

