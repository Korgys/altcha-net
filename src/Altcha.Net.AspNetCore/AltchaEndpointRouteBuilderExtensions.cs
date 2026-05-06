using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Altcha.Net.AspNetCore;

public static class AltchaEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapAltchaChallenge(this IEndpointRouteBuilder endpoints, string pattern = "/altcha/challenge")
    {
        if (endpoints == null)
        {
            throw new ArgumentNullException(nameof(endpoints));
        }

        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("The ALTCHA challenge endpoint pattern is required.", nameof(pattern));
        }

        return endpoints.MapGet(pattern, (AltchaService altcha) => Results.Json(altcha.GenerateChallenge()));
    }
}
