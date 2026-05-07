using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;

namespace Altcha.Net.AspNetCore;

public sealed class DistributedCacheAltchaReplayStore : IAltchaReplayStore, IAltchaReplayStoreAsync
{
    private const string CacheValue = "1";
    private readonly IDistributedCache _cache;
    private readonly IAtomicAltchaReplayStore? _atomicStore;
    private readonly string _keyPrefix;

    public DistributedCacheAltchaReplayStore(IDistributedCache cache)
        : this(cache, "altcha:replay:")
    {
    }

    public DistributedCacheAltchaReplayStore(IDistributedCache cache, string keyPrefix)
        : this(cache, null, keyPrefix)
    {
    }

    public DistributedCacheAltchaReplayStore(
        IDistributedCache cache,
        IAtomicAltchaReplayStore? atomicStore,
        string keyPrefix = "altcha:replay:")
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _atomicStore = atomicStore;
        _keyPrefix = string.IsNullOrWhiteSpace(keyPrefix)
            ? throw new ArgumentException("The replay cache key prefix is required.", nameof(keyPrefix))
            : keyPrefix;
    }

    public bool TryStoreOnce(string key, DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("The replay key is required.", nameof(key));
        }

        var now = DateTimeOffset.UtcNow;
        if (expiresAt <= now)
        {
            return false;
        }

        var cacheKey = _keyPrefix + HashKey(key);

        if (_atomicStore != null)
        {
            return _atomicStore.TryStoreOnceAtomic(cacheKey, expiresAt);
        }

        // Best-effort fallback for generic IDistributedCache providers.
        // This path is not strictly atomic across concurrent workers.
        if (_cache.GetString(cacheKey) != null)
        {
            return false;
        }

        _cache.SetString(cacheKey, CacheValue, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = expiresAt
        });

        return true;
    }

    public async ValueTask<bool> TryStoreOnceAsync(string key, DateTimeOffset expiresAt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("The replay key is required.", nameof(key));
        }

        var now = DateTimeOffset.UtcNow;
        if (expiresAt <= now)
        {
            return false;
        }

        var cacheKey = _keyPrefix + HashKey(key);

        if (_atomicStore != null)
        {
            return _atomicStore.TryStoreOnceAtomic(cacheKey, expiresAt);
        }

        var existing = await _cache.GetStringAsync(cacheKey, ct).ConfigureAwait(false);
        if (existing != null)
        {
            return false;
        }

        await _cache.SetStringAsync(cacheKey, CacheValue, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = expiresAt
        }, ct).ConfigureAwait(false);

        return true;
    }

    private static string HashKey(string key)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
