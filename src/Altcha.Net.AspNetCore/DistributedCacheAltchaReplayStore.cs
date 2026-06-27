using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;

namespace Altcha.Net.AspNetCore;

/// <summary>
/// Stores ALTCHA replay keys in an ASP.NET Core distributed cache.
/// </summary>
public sealed class DistributedCacheAltchaReplayStore : IAltchaReplayStore
{
    private const string CacheValue = "1";
    private readonly IDistributedCache _cache;
    private readonly IAtomicAltchaReplayStore? _atomicStore;
    private readonly string _keyPrefix;

    /// <summary>
    /// Creates a distributed replay store with the default key prefix.
    /// </summary>
    /// <param name="cache">The distributed cache used for replay keys.</param>
    public DistributedCacheAltchaReplayStore(IDistributedCache cache)
        : this(cache, "altcha:replay:")
    {
    }

    /// <summary>
    /// Creates a distributed replay store with a custom key prefix.
    /// </summary>
    /// <param name="cache">The distributed cache used for replay keys.</param>
    /// <param name="keyPrefix">The cache key prefix used to isolate ALTCHA replay keys.</param>
    public DistributedCacheAltchaReplayStore(IDistributedCache cache, string keyPrefix)
        : this(cache, null, keyPrefix)
    {
    }

    /// <summary>
    /// Creates a distributed replay store with an optional atomic store for strict replay protection.
    /// </summary>
    /// <param name="cache">The distributed cache used for replay keys.</param>
    /// <param name="atomicStore">The optional atomic store used to insert replay keys once across workers.</param>
    /// <param name="keyPrefix">The cache key prefix used to isolate ALTCHA replay keys.</param>
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

    /// <summary>
    /// Tries to store a replay key until its expiry time.
    /// </summary>
    /// <param name="key">The replay key, usually the challenge hash.</param>
    /// <param name="expiresAt">The time after which the replay key may be discarded.</param>
    /// <returns><c>true</c> when the key was stored; otherwise <c>false</c> for a replay or expired key.</returns>
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

    private static string HashKey(string key)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
