using StackExchange.Redis;

namespace Altcha.Net.AspNetCore;

/// <summary>
/// Uses Redis SET NX with an expiry for atomic ALTCHA replay protection.
/// </summary>
public sealed class RedisAltchaReplayStore : IAtomicAltchaReplayStore
{
    private const string CacheValue = "1";
    private readonly IDatabase _database;

    /// <summary>
    /// Creates a Redis-backed atomic replay store.
    /// </summary>
    /// <param name="database">The Redis database used for replay keys.</param>
    public RedisAltchaReplayStore(IDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    /// <summary>
    /// Tries to store a replay key atomically until its expiry time.
    /// </summary>
    /// <param name="key">The replay key to store.</param>
    /// <param name="expiresAt">The time after which the replay key may be discarded.</param>
    /// <returns><c>true</c> when Redis inserted the key; otherwise <c>false</c>.</returns>
    public bool TryStoreOnceAtomic(string key, DateTimeOffset expiresAt)
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

        var ttl = expiresAt - now;
        return _database.StringSet(key, CacheValue, expiry: ttl, when: When.NotExists);
    }
}
