using System.Threading.RateLimiting;
using CommunityStarter.Api.Endpoints;
using CommunityStarter.Api.Infrastructure;
using CommunityStarter.Api.Realtime;
using CommunityStarter.Infrastructure;
using CommunityStarter.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 16 * 1024;
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("authentication", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            QueueLimit = 0,
            Window = TimeSpan.FromMinutes(1)
        }));
});
builder.Services.AddHealthChecks().AddCheck<DatabaseReadinessCheck>("database", tags: ["ready"]);
builder.Services.AddCommunityStarterInfrastructure(builder.Configuration);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("community-starter"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

WebApplication app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseMiddleware<SafeProblemDetailsMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context => CachePolicy.Apply(context.Context.Response, context.File.Name)
});
app.UseMiddleware<CurrentAccountMiddleware>();
app.UseMiddleware<CsrfMiddleware>();

if (app.Configuration.GetValue<bool>("Migration:ApplyOnStartup"))
{
    await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
    CommunityDbContext dbContext = scope.ServiceProvider.GetRequiredService<CommunityDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.MapOpenApi("/api/openapi.json");
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});
app.MapHub<CommunityHub>("/hubs/community");
app.MapCommunityStarterApi();

app.MapFallback(async context =>
{
    string path = context.Request.Path.Value ?? string.Empty;
    if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/hubs", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
        Path.HasExtension(path))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    string webRoot = Path.GetFullPath(app.Environment.WebRootPath ?? "wwwroot");
    string relative;
    if (path.Equals("/app", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/app/", StringComparison.OrdinalIgnoreCase))
    {
        relative = Path.Combine("app", "index.html");
    }
    else
    {
        string publicRoute = path.Trim('/');
        relative = string.IsNullOrEmpty(publicRoute)
            ? "index.html"
            : Path.Combine(publicRoute, "index.html");
    }

    string file = Path.GetFullPath(Path.Combine(webRoot, relative));
    string rootedPrefix = webRoot.EndsWith(Path.DirectorySeparatorChar)
        ? webRoot
        : webRoot + Path.DirectorySeparatorChar;
    if (!file.StartsWith(rootedPrefix, StringComparison.OrdinalIgnoreCase) || !File.Exists(file))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    context.Response.ContentType = "text/html; charset=utf-8";
    context.Response.Headers.CacheControl = "no-cache";
    await context.Response.SendFileAsync(file);
});

await app.RunAsync();

public partial class Program;
