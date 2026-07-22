using CommunityStarter.Api.Infrastructure;
using CommunityStarter.Application.Contracts;
using CommunityStarter.Application.Services;
using CommunityStarter.Domain.GeneratedFeatures;

namespace CommunityStarter.Api.Endpoints;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapCommunityStarterApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder api = endpoints.MapGroup("/api");

        api.MapPost("/accounts", RegisterAsync).RequireRateLimiting("authentication");
        api.MapPost("/accounts/verify", VerifyAsync).RequireRateLimiting("authentication");
        api.MapPost("/sessions", SignInAsync).RequireRateLimiting("authentication");
        api.MapDelete("/sessions/current", SignOutAsync);
        api.MapGet("/me", (HttpContext context) => Results.Ok(context.RequireCurrentAccount()));

        api.MapGet("/communities", async (int? take, CommunityService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.ListPublicAsync(take ?? 20, cancellationToken)));
        api.MapPost("/communities", CreateCommunityAsync);
        api.MapPatch("/communities/{communityId:guid}", ConfigureCommunityAsync);
        api.MapPost("/communities/{communityId:guid}/invitations", InviteAsync);
        api.MapPost("/invitations/accept", AcceptInvitationAsync);
        api.MapGet("/communities/{communityId:guid}/feed", FeedAsync);
        api.MapPost("/communities/{communityId:guid}/posts", PublishPostAsync);
        api.MapPut("/communities/{communityId:guid}/posts/{postId:guid}", EditPostAsync);
        api.MapPost("/communities/{communityId:guid}/posts/{postId:guid}/reactions", ReactAsync);
        api.MapPost("/communities/{communityId:guid}/posts/{postId:guid}/reports", ReportAsync);
        api.MapPost("/communities/{communityId:guid}/moderation-cases/{caseId:guid}/actions", ModerateAsync);

        api.MapGet("/features", () => Results.Ok(GeneratedFeatureCatalog.Features));
        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterAccountCommand command,
        IdentityService service,
        IHostEnvironment environment,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        RegisterAccountResult result = await service.RegisterAsync(command, cancellationToken);
        object response = environment.IsDevelopment() && configuration.GetValue<bool>("DevelopmentActions:ExposeTokens")
            ? new { result.AccountId, result.Version, result.VerificationToken, verificationRequired = true }
            : new { result.AccountId, result.Version, verificationRequired = true };
        return Results.Created($"/api/accounts/{result.AccountId:D}", response);
    }

    private static async Task<IResult> VerifyAsync(
        VerifyAccountCommand command,
        IdentityService service,
        CancellationToken cancellationToken)
    {
        await service.VerifyAsync(command, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> SignInAsync(
        SignInCommand command,
        IdentityService service,
        Application.Abstractions.ISecretService secrets,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        SignInResult result = await service.SignInAsync(command, cancellationToken);
        context.Response.Cookies.Append(HttpContextKeys.SessionCookie, result.SessionToken, SessionCookie(result.ExpiresAt));
        string csrf = secrets.GenerateToken();
        context.Response.Cookies.Append(HttpContextKeys.CsrfCookie, csrf, CsrfCookie(result.ExpiresAt));
        return Results.Ok(new { result.AccountId, result.ExpiresAt, csrfToken = csrf });
    }

    private static async Task<IResult> SignOutAsync(
        HttpContext context,
        IdentityService service,
        CancellationToken cancellationToken)
    {
        _ = context.RequireCurrentAccount();
        await service.SignOutAsync(
            context.Request.Cookies[HttpContextKeys.SessionCookie],
            cancellationToken);
        context.Response.Cookies.Delete(HttpContextKeys.SessionCookie, SessionCookie(DateTimeOffset.UnixEpoch));
        context.Response.Cookies.Delete(HttpContextKeys.CsrfCookie, CsrfCookie(DateTimeOffset.UnixEpoch));
        return Results.NoContent();
    }

    private static async Task<IResult> CreateCommunityAsync(
        CreateCommunityCommand command,
        CommunityService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        CurrentAccount account = context.RequireCurrentAccount();
        CommunityResult result = await service.CreateAsync(account.Id, command, cancellationToken);
        return Results.Created($"/api/communities/{result.Id:D}", result);
    }

    private static async Task<IResult> ConfigureCommunityAsync(
        Guid communityId,
        ConfigureCommunityCommand command,
        CommunityService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        CurrentAccount account = context.RequireCurrentAccount();
        return Results.Ok(await service.ConfigureAsync(account.Id, communityId, command, cancellationToken));
    }

    private static async Task<IResult> InviteAsync(
        Guid communityId,
        InviteMemberCommand command,
        CommunityService service,
        HttpContext context,
        IHostEnvironment environment,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        CurrentAccount account = context.RequireCurrentAccount();
        InvitationResult invitation = await service.InviteAsync(account.Id, communityId, command, cancellationToken);
        object response = environment.IsDevelopment() && configuration.GetValue<bool>("DevelopmentActions:ExposeTokens")
            ? invitation
            : new { invitation.InvitationId, invitation.ExpiresAt };
        return Results.Created($"/api/communities/{communityId:D}/invitations/{invitation.InvitationId:D}", response);
    }

    private static async Task<IResult> AcceptInvitationAsync(
        AcceptInvitationCommand command,
        CommunityService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        CurrentAccount account = context.RequireCurrentAccount();
        await service.AcceptInvitationAsync(account.Id, command, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> FeedAsync(
        Guid communityId,
        string? cursor,
        int? take,
        ContentService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        CurrentAccount account = context.RequireCurrentAccount();
        return Results.Ok(await service.FeedAsync(account.Id, communityId, cursor, take ?? 20, cancellationToken));
    }

    private static async Task<IResult> PublishPostAsync(
        Guid communityId,
        PublishPostCommand command,
        ContentService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        CurrentAccount account = context.RequireCurrentAccount();
        PostResult post = await service.PublishAsync(account.Id, communityId, command, cancellationToken);
        return Results.Created($"/api/communities/{communityId:D}/posts/{post.Id:D}", post);
    }

    private static async Task<IResult> EditPostAsync(
        Guid communityId,
        Guid postId,
        EditPostCommand command,
        ContentService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        CurrentAccount account = context.RequireCurrentAccount();
        return Results.Ok(await service.EditAsync(account.Id, communityId, postId, command, cancellationToken));
    }

    private static async Task<IResult> ReactAsync(
        Guid communityId,
        Guid postId,
        AddReactionCommand command,
        ContentService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        CurrentAccount account = context.RequireCurrentAccount();
        await service.AddReactionAsync(account.Id, communityId, postId, command, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ReportAsync(
        Guid communityId,
        Guid postId,
        SubmitReportCommand command,
        ContentService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        CurrentAccount account = context.RequireCurrentAccount();
        ReportResult report = await service.ReportAsync(account.Id, communityId, postId, command, cancellationToken);
        return Results.Created($"/api/communities/{communityId:D}/moderation-cases/{report.CaseId:D}", report);
    }

    private static async Task<IResult> ModerateAsync(
        Guid communityId,
        Guid caseId,
        ApplyModerationActionCommand command,
        ModerationService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        CurrentAccount account = context.RequireCurrentAccount();
        ModerationActionResult result = await service.ApplyActionAsync(
            account.Id,
            communityId,
            caseId,
            command,
            cancellationToken);
        return Results.Created(
            $"/api/communities/{communityId:D}/moderation-cases/{caseId:D}/actions/{result.ActionId:D}",
            result);
    }

    private static CookieOptions SessionCookie(DateTimeOffset expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        Expires = expiresAt,
        IsEssential = true
    };

    private static CookieOptions CsrfCookie(DateTimeOffset expiresAt) => new()
    {
        HttpOnly = false,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        Expires = expiresAt,
        IsEssential = true
    };
}
