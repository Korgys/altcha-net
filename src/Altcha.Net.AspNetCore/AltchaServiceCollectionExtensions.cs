using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Altcha.Net.AspNetCore;

/// <summary>
/// Registers ALTCHA services and replay stores with an ASP.NET Core service collection.
/// </summary>
public static class AltchaServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="AltchaService"/> and configures ALTCHA options in code.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The options callback; set a private <see cref="AltchaOptions.SecretKey"/> before use.</param>
    /// <returns>The service collection for chaining.</returns>
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

    /// <summary>
    /// Registers <see cref="AltchaService"/> and binds ALTCHA options from configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration section containing ALTCHA options, including the private secret key.</param>
    /// <returns>The service collection for chaining.</returns>
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

    /// <summary>
    /// Replaces the default in-memory replay store with an <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/> based store.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="mode">The replay protection mode; use strict atomic mode for multi-node deployments when an atomic store is registered.</param>
    /// <returns>The service collection for chaining.</returns>
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

    /// <summary>
    /// Registers a Redis-backed atomic replay store for strict replay protection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="databaseFactory">A factory that returns the Redis database used for replay keys.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRedisAltchaReplayStore(
        this IServiceCollection services,
        Func<IServiceProvider, IDatabase> databaseFactory)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (databaseFactory == null)
        {
            throw new ArgumentNullException(nameof(databaseFactory));
        }

        services.AddSingleton<IAtomicAltchaReplayStore>(sp => new RedisAltchaReplayStore(databaseFactory(sp)));
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
