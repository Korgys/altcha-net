using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;

namespace Altcha.Net.AspNetCore;

/// <summary>
/// Adds ALTCHA challenge endpoints to ASP.NET Core route builders.
/// </summary>
public static class AltchaEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Configures security-related behavior for the generated challenge endpoint.
    /// </summary>
    public sealed class AltchaChallengeEndpointSecurityOptions
    {
        /// <summary>
        /// Gets or sets whether challenge responses should include no-store cache headers.
        /// </summary>
        public bool DisableResponseCaching { get; set; } = true;

        /// <summary>
        /// Gets or sets the ASP.NET Core rate limiting policy applied to the challenge endpoint.
        /// </summary>
        public string? RateLimitingPolicyName { get; set; }

        /// <summary>
        /// Gets or sets the allowed host names for the challenge endpoint.
        /// </summary>
        public string[]? AllowedHosts { get; set; }
    }

    /// <summary>
    /// Maps the default ALTCHA challenge endpoint.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern used to serve challenges.</param>
    /// <returns>The mapped endpoint convention builder.</returns>
    public static IEndpointConventionBuilder MapAltchaChallenge(this IEndpointRouteBuilder endpoints, string pattern = "/altcha/challenge")
        => endpoints.MapAltchaChallenge(pattern, configureSecurity: null);

    /// <summary>
    /// Maps an ALTCHA challenge endpoint and allows security options such as no-store caching, rate limiting, and host restrictions.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern used to serve challenges.</param>
    /// <param name="configureSecurity">An optional callback for endpoint security options.</param>
    /// <returns>The mapped endpoint convention builder.</returns>
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
