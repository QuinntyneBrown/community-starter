using System.Text.Json;
using CommunityStarter.Application.Contracts;
using CommunityStarter.Application.Services;
using CommunityStarter.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CommunityStarter.Api.Infrastructure;

public static class HttpContextKeys
{
    public const string CurrentAccount = "CommunityStarter.CurrentAccount";
    public const string SessionCookie = "cs_session";
    public const string CsrfCookie = "cs_csrf";
    public const string CsrfHeader = "X-CSRF-Token";
}

public sealed class CurrentAccountMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IdentityService identityService)
    {
        string? token = context.Request.Cookies[HttpContextKeys.SessionCookie];
        CurrentAccount? account = await identityService.AuthenticateAsync(token, context.RequestAborted);
        if (account is not null)
        {
            context.Items[HttpContextKeys.CurrentAccount] = account;
        }

        await next(context);
    }
}

public sealed class CsrfMiddleware(RequestDelegate next)
{
    private static readonly string[] ExemptPaths =
    [
        "/api/accounts",
        "/api/accounts/verify",
        "/api/sessions"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method) ||
            HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsOptions(context.Request.Method) ||
            ExemptPaths.Contains(context.Request.Path.Value, StringComparer.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        string? cookie = context.Request.Cookies[HttpContextKeys.CsrfCookie];
        string? header = context.Request.Headers[HttpContextKeys.CsrfHeader].FirstOrDefault();
        if (string.IsNullOrEmpty(cookie) ||
            string.IsNullOrEmpty(header) ||
            !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(cookie),
                System.Text.Encoding.UTF8.GetBytes(header)))
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status400BadRequest,
                "csrf_invalid",
                "The request could not be verified.");
            return;
        }

        await next(context);
    }

    private static Task WriteProblemAsync(HttpContext context, int status, string code, string title)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        return context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = $"https://community-starter.invalid/problems/{code}",
            Extensions = { ["code"] = code, ["traceId"] = context.TraceIdentifier }
        });
    }
}

public sealed class SafeProblemDetailsMiddleware(RequestDelegate next, ILogger<SafeProblemDetailsMiddleware> logger)
{
    private static readonly Action<ILogger, string, Exception?> ConcurrencyLog = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(1001, "ConcurrencyConflict"),
        "A conditional database update conflicted for {TraceId}.");
    private static readonly Action<ILogger, string, Exception?> ConstraintLog = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(1002, "ConstraintConflict"),
        "A unique database constraint conflicted for {TraceId}.");
    private static readonly Action<ILogger, string, Exception?> UnexpectedLog = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(1003, "UnexpectedRequestFailure"),
        "Unhandled request failure for {TraceId}.");

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException exception)
        {
            int status = StatusFor(exception.Code);
            await WriteProblemAsync(context, status, exception.Code, exception.Message);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            ConcurrencyLog(logger, context.TraceIdentifier, exception);
            await WriteProblemAsync(
                context,
                StatusCodes.Status409Conflict,
                "concurrency_conflict",
                "The resource changed. Refresh and try again.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgres && postgres.SqlState == "23505")
        {
            ConstraintLog(logger, context.TraceIdentifier, exception);
            await WriteProblemAsync(
                context,
                StatusCodes.Status409Conflict,
                "resource_conflict",
                "The requested change conflicts with current state.");
        }
        catch (Exception exception)
        {
            UnexpectedLog(logger, context.TraceIdentifier, exception);
            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "unexpected_failure",
                "The request could not be completed.");
        }
    }

    private static int StatusFor(string code) => code switch
    {
        "sign_in_failed" or "authentication_required" => StatusCodes.Status401Unauthorized,
        "permission_denied" => StatusCodes.Status403Forbidden,
        "community_not_found" or "post_not_found" or "moderation_case_not_found" or "account_not_found" =>
            StatusCodes.Status404NotFound,
        "concurrency_conflict" or "account_exists" or "community_slug_unavailable" or "membership_exists" or
            "case_already_resolved" => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest
    };

    private static Task WriteProblemAsync(HttpContext context, int status, string code, string title)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        ProblemDetails problem = new()
        {
            Status = status,
            Title = title,
            Type = $"https://community-starter.invalid/problems/{code}"
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        return context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonSerializerOptions.Web));
    }
}

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            IHeaderDictionary headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers.ContentSecurityPolicy =
                "default-src 'self'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'; " +
                "img-src 'self' data:; font-src 'self'; style-src 'self'; script-src 'self'; connect-src 'self' wss:";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
            if (context.Request.IsHttps)
            {
                headers.StrictTransportSecurity = "max-age=31536000; includeSubDomains";
            }

            return Task.CompletedTask;
        });
        await next(context);
    }
}

public static class HttpContextAccountExtensions
{
    public static CurrentAccount RequireCurrentAccount(this HttpContext context) =>
        context.Items.TryGetValue(HttpContextKeys.CurrentAccount, out object? value) && value is CurrentAccount account
            ? account
            : throw new DomainException("authentication_required", "Sign in is required.");
}

public static class CachePolicy
{
    public static void Apply(HttpResponse response, string fileName)
    {
        bool immutable = System.Text.RegularExpressions.Regex.IsMatch(
            fileName,
            @"(?:[-.])[a-z0-9_-]{7,}\.[^.]+$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        response.Headers.CacheControl = immutable
            ? "public,max-age=31536000,immutable"
            : "no-cache";
    }
}
