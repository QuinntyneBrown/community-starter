using System.Text.Json;
using CommunityStarter.Application.GeneratedFeatures;
using CommunityStarter.Domain.Common;
using CommunityStarter.Domain.Operations;
using CommunityStarter.Infrastructure.Persistence;

namespace CommunityStarter.Infrastructure.Jobs;

public sealed class FeatureProjectionSink(CommunityDbContext dbContext, IClock clock) : IFeatureProjectionSink
{
    public async ValueTask EnqueueAsync(
        string featureSlug,
        Guid subjectId,
        Guid? communityId,
        string requirementId,
        long committedVersion,
        CancellationToken cancellationToken)
    {
        dbContext.Jobs.Add(DurableJob.Enqueue(
            "feature.project",
            JsonSerializer.Serialize(new
            {
                featureSlug,
                subjectId,
                communityId,
                requirementId,
                committedVersion
            }),
            clock.UtcNow));
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

