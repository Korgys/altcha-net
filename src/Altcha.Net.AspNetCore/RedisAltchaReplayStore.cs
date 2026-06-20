using StackExchange.Redis;

namespace Altcha.Net.AspNetCore;

public sealed class RedisAltchaReplayStore : IAtomicAltchaReplayStore
{
    private const string CacheValue = "1";
    private readonly IDatabase _database;

    public RedisAltchaReplayStore(IDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

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
