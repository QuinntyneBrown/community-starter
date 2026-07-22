using CommunityStarter.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CommunityStarter.Api.Infrastructure;

public sealed class DatabaseReadinessCheck(CommunityDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            return await dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("The database is not reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("The database readiness check failed.", exception);
        }
    }
}

