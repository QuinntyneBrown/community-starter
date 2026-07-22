using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CommunityStarter.IntegrationTests;

public sealed class ApiContractTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient client;

    public ApiContractTests(ApiFactory factory)
    {
        client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task CapabilityCatalogIsPublicAndComplete()
    {
        HttpResponseMessage response = await client.GetAsync("/api/features");
        FeatureContract[]? features = await response.Content.ReadFromJsonAsync<FeatureContract[]>(
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(features);
        Assert.Equal(82, features.Length);
        Assert.Equal(260, features.Sum(feature => feature.Operations.Length));
        Assert.Contains("nosniff", response.Headers.GetValues("X-Content-Type-Options"));
    }

    [Fact]
    public async Task ProtectedEndpointUsesProblemDetailsAndUnauthorizedStatus()
    {
        HttpResponseMessage response = await client.GetAsync("/api/me");
        ProblemContract? problem = await response.Content.ReadFromJsonAsync<ProblemContract>(
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("authentication_required", problem?.Code);
        Assert.False(string.IsNullOrWhiteSpace(problem?.TraceId));
    }

    private sealed record FeatureContract(OperationContract[] Operations);
    private sealed record OperationContract(string RequirementId, string Method);
    private sealed record ProblemContract(string? Code, string? TraceId);
}

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Community"] = "Host=127.0.0.1;Database=unused;Username=unused;Password=unused",
                ["Security:TokenPepper"] = "integration-test-pepper-is-at-least-thirty-two-characters",
                ["Migration:ApplyOnStartup"] = "false",
                ["Runtime:Role"] = "api"
            }));
    }
}
