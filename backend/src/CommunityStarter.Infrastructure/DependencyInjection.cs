using CommunityStarter.Application.Abstractions;
using CommunityStarter.Application.GeneratedFeatures;
using CommunityStarter.Application.Services;
using CommunityStarter.Domain.Common;
using CommunityStarter.Infrastructure.Jobs;
using CommunityStarter.Infrastructure.Persistence;
using CommunityStarter.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityStarter.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCommunityStarterInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        string connectionString = configuration.GetConnectionString("Community")
            ?? throw new InvalidOperationException("ConnectionStrings:Community is required.");

        services.AddDbContext<CommunityDbContext>(options => options.UseNpgsql(connectionString));
        services.Configure<SecurityOptions>(configuration.GetSection(SecurityOptions.SectionName));
        services.Configure<DurableJobOptions>(configuration.GetSection(DurableJobOptions.SectionName));
        services.AddScoped<IPlatformStore, PlatformStore>();
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<ISecretService, SecretService>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ICorrelationContext, CorrelationContext>();
        services.AddScoped<IFeatureProjectionSink, FeatureProjectionSink>();
        services.AddScoped<IDurableJobDispatcher, DurableJobDispatcher>();
        services.AddScoped<IdentityService>();
        services.AddScoped<CommunityService>();
        services.AddScoped<ContentService>();
        services.AddScoped<ModerationService>();
        if (!string.Equals(configuration["Runtime:Role"], "api", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHostedService<DurableJobWorker>();
        }

        return services;
    }
}
