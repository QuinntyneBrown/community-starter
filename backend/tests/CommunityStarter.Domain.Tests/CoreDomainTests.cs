using CommunityStarter.Domain.Common;
using CommunityStarter.Domain.Communities;
using CommunityStarter.Domain.Content;
using CommunityStarter.Domain.GeneratedFeatures;
using CommunityStarter.Domain.Identity;
using CommunityStarter.Domain.Operations;

namespace CommunityStarter.Domain.Tests;

public sealed class CoreDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void VerificationRequiresTheObservedVersion()
    {
        Account account = Account.Register("member@example.org", "hash", Now);

        DomainException exception = Assert.Throws<DomainException>(() => account.Verify(1, Now.AddMinutes(1)));

        Assert.Equal("concurrency_conflict", exception.Code);
        Assert.Equal(AccountStatus.PendingVerification, account.Status);
    }

    [Fact]
    public void CommunityRolesKeepManagementServerOwned()
    {
        Guid communityId = Guid.CreateVersion7();
        Membership owner = Membership.CreateOwner(communityId, Guid.CreateVersion7(), Now);
        Membership member = Membership.Join(communityId, Guid.CreateVersion7(), Now);

        Assert.True(owner.Can("community.manage"));
        Assert.True(owner.Can("content.moderate"));
        Assert.False(member.Can("community.manage"));
        Assert.True(member.Can("content.publish"));
    }

    [Fact]
    public void OnlyAuthorsOrModeratorsCanEditPosts()
    {
        Guid authorId = Guid.CreateVersion7();
        Post post = Post.Publish(Guid.CreateVersion7(), authorId, "Original", Now);

        DomainException exception = Assert.Throws<DomainException>(() =>
            post.Edit(Guid.CreateVersion7(), "Changed", post.Version, false, Now.AddMinutes(1)));

        Assert.Equal("permission_denied", exception.Code);
        Assert.Equal("Original", post.Body);
    }

    [Fact]
    public void JobRetriesAreDeterministicAndBounded()
    {
        DurableJob first = DurableJob.Enqueue("projection", "{}", Now);
        first.Lease("worker-1", Now, TimeSpan.FromMinutes(1));
        first.Fail("transient", Now, 3);

        Assert.Equal(JobStatus.Retrying, first.Status);
        Assert.InRange(first.AvailableAt, Now.AddSeconds(2), Now.AddSeconds(3));
    }

    [Fact]
    public void ExpiredJobLeasesCanBeRecovered()
    {
        DurableJob job = DurableJob.Enqueue("projection", "{}", Now);
        job.Lease("worker-1", Now, TimeSpan.FromSeconds(30));

        job.RecoverExpiredLease(Now.AddSeconds(31));

        Assert.Equal(JobStatus.Retrying, job.Status);
        Assert.Null(job.LeaseOwner);
        Assert.Equal(Now.AddSeconds(31), job.AvailableAt);
    }

    [Fact]
    public void GeneratedCatalogTracesEveryDetailedDesignOperation()
    {
        Assert.Equal(82, GeneratedFeatureCatalog.Features.Count);
        Assert.Equal(260, GeneratedFeatureCatalog.Features.Sum(feature => feature.Operations.Count));
        Assert.Equal(82, GeneratedFeatureCatalog.Features.Select(feature => feature.Slug).Distinct().Count());
        Assert.Equal(
            260,
            GeneratedFeatureCatalog.Features
                .SelectMany(feature => feature.Operations)
                .Select(operation => operation.RequirementId)
                .Distinct()
                .Count());
    }
}
