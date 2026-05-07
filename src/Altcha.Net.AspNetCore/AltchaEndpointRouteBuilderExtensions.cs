using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;

namespace Altcha.Net.AspNetCore;

public static class AltchaEndpointRouteBuilderExtensions
{
    public sealed class AltchaChallengeEndpointSecurityOptions
    {
        public bool DisableResponseCaching { get; set; } = true;

        public string? RateLimitingPolicyName { get; set; }

        public string[]? AllowedHosts { get; set; }
    }

    public static IEndpointConventionBuilder MapAltchaChallenge(this IEndpointRouteBuilder endpoints, string pattern = "/altcha/challenge")
        => endpoints.MapAltchaChallenge(pattern, configureSecurity: null);

    public static IEndpointConventionBuilder MapAltchaChallenge(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Action<AltchaChallengeEndpointSecurityOptions>? configureSecurity)
    {
        if (endpoints == null)
        {
            throw new ArgumentNullException(nameof(endpoints));
        }

        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("The ALTCHA challenge endpoint pattern is required.", nameof(pattern));
        }

        var securityOptions = new AltchaChallengeEndpointSecurityOptions();
        configureSecurity?.Invoke(securityOptions);

        var endpoint = endpoints.MapGet(pattern, (HttpContext httpContext, AltchaService altcha) =>
        {
            if (securityOptions.DisableResponseCaching)
            {
                httpContext.Response.Headers.CacheControl = "no-store";
                httpContext.Response.Headers[HeaderNames.Pragma] = "no-cache";
                httpContext.Response.Headers[HeaderNames.Expires] = "0";
            }

            return Results.Json(altcha.GenerateChallenge());
        });

        if (!string.IsNullOrWhiteSpace(securityOptions.RateLimitingPolicyName))
        {
            endpoint.RequireRateLimiting(securityOptions.RateLimitingPolicyName);
        }

        if (securityOptions.AllowedHosts is { Length: > 0 })
        {
            endpoint.RequireHost(securityOptions.AllowedHosts);
        }

        return endpoint;
    }
}
