using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;

namespace Altcha.Net.AspNetCore;

public sealed class DistributedCacheAltchaReplayStore : IAltchaReplayStore
{
    private const string CacheValue = "1";
    private readonly IDistributedCache _cache;
    private readonly string _keyPrefix;

    public DistributedCacheAltchaReplayStore(IDistributedCache cache)
        : this(cache, "altcha:replay:")
    {
    }

    public DistributedCacheAltchaReplayStore(IDistributedCache cache, string keyPrefix)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
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
