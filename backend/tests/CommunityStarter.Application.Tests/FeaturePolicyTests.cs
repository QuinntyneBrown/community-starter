using CommunityStarter.Domain.Common;

namespace CommunityStarter.Application.Tests;

public sealed class FeaturePolicyTests
{
    private static readonly Guid SubjectId = Guid.Parse("0190f0d0-0000-7000-8000-000000000001");

    [Fact]
    public void AcceptedChangesAdvanceExactlyOneVersion()
    {
        FeatureDecision decision = Evaluate(available: true, authorized: true, recordVersion: 7, expectedVersion: 7);

        Assert.True(decision.IsAccepted);
        Assert.Equal(8, decision.ResultingVersion);
        Assert.Equal("publish", decision.ResultingState);
    }

    [Theory]
    [InlineData(false, true, 7, 7, FeatureDecisionKind.NotFound, "resource_not_found")]
    [InlineData(true, false, 7, 7, FeatureDecisionKind.Forbidden, "permission_denied")]
    [InlineData(true, true, 7, 6, FeatureDecisionKind.Conflict, "concurrency_conflict")]
    public void RejectionsAreStable(
        bool available,
        bool authorized,
        long recordVersion,
        long expectedVersion,
        FeatureDecisionKind kind,
        string code)
    {
        FeatureDecision decision = Evaluate(available, authorized, recordVersion, expectedVersion);

        Assert.Equal(kind, decision.Kind);
        Assert.Equal(code, decision.Code);
        Assert.Equal(recordVersion, decision.ResultingVersion);
    }

    private static FeatureDecision Evaluate(
        bool available,
        bool authorized,
        long recordVersion,
        long expectedVersion) => FeaturePolicyEvaluator.Evaluate(
            "L2-TEST-001",
            new FeatureState(SubjectId, null, "draft", recordVersion, authorized, available),
            new FeatureInput(SubjectId, "publish", expectedVersion, null, new Dictionary<string, string>()));
}
