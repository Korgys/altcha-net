using System.Collections.Concurrent;

namespace Altcha.Net;

public sealed class MemoryAltchaReplayStore : IAltchaReplayStore
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _usedChallenges = new ConcurrentDictionary<string, DateTimeOffset>(StringComparer.Ordinal);
    private int _operations;

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

        if (Interlocked.Increment(ref _operations) % 256 == 0)
        {
            RemoveExpired(now);
        }

        while (true)
        {
            if (_usedChallenges.TryGetValue(key, out var existingExpiresAt))
            {
                if (existingExpiresAt > now)
                {
                    return false;
                }

                _usedChallenges.TryRemove(key, out _);
                continue;
            }

            if (_usedChallenges.TryAdd(key, expiresAt))
            {
                return true;
            }
        }
    }

    private void RemoveExpired(DateTimeOffset now)
    {
        foreach (var item in _usedChallenges)
        {
            if (item.Value <= now)
            {
                _usedChallenges.TryRemove(item.Key, out _);
            }
        }
    }
}
