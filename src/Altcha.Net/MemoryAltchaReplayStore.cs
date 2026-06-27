using System.Collections.Concurrent;
using System.Threading;

namespace Altcha.Net;

/// <summary>
/// Stores replay keys in memory for single-process applications.
/// </summary>
public sealed class MemoryAltchaReplayStore : IAltchaReplayStore
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _usedChallenges = new ConcurrentDictionary<string, DateTimeOffset>(StringComparer.Ordinal);
    private int _operations;

    /// <summary>
    /// Tries to store a replay key if it has not already been used in this process.
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
