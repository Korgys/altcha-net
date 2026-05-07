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

    public static IServiceCollection AddDistributedAltchaReplayStore(
        this IServiceCollection services,
        DistributedAltchaReplayStoreMode mode = DistributedAltchaReplayStoreMode.BestEffort)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddSingleton<IAltchaReplayStore>(sp =>
        {
            var cache = sp.GetRequiredService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();

            if (mode == DistributedAltchaReplayStoreMode.StrictAtomic)
            {
                var atomic = sp.GetService<IAtomicAltchaReplayStore>()
                    ?? throw new InvalidOperationException(
                        "StrictAtomic mode requires an IAtomicAltchaReplayStore implementation (for example a Redis SET NX EX adapter).");

                return new DistributedCacheAltchaReplayStore(cache, atomic);
            }

            return new DistributedCacheAltchaReplayStore(cache);
        });
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
