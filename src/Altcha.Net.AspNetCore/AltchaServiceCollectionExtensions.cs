using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Altcha.Net.AspNetCore;

public static class AltchaServiceCollectionExtensions
{
    public static IServiceCollection AddAltcha(this IServiceCollection services, Action<AltchaOptions> configure)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        services.Configure(configure);
        return services.AddAltchaCore();
    }

    public static IServiceCollection AddAltcha(this IServiceCollection services, IConfiguration configuration)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        services.Configure<AltchaOptions>(configuration);
        return services.AddAltchaCore();
    }

    public static IServiceCollection AddDistributedAltchaReplayStore(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddSingleton<IAltchaReplayStore, DistributedCacheAltchaReplayStore>();
        return services;
    }

    private static IServiceCollection AddAltchaCore(this IServiceCollection services)
    {
        services.AddOptions<AltchaOptions>();
        services.TryAddSingleton<IAltchaReplayStore, MemoryAltchaReplayStore>();
        services.TryAddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AltchaOptions>>().Value;
            var replayStore = sp.GetRequiredService<IAltchaReplayStore>();
            return new AltchaService(options, replayStore);
        });

        return services;
    }
}
