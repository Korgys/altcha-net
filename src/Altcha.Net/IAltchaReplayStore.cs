namespace Altcha.Net;

/// <summary>
/// Stores used ALTCHA challenge keys so each challenge can be accepted only once.
/// </summary>
public interface IAltchaReplayStore
{
    /// <summary>
    /// Tries to store a replay key if it has not already been used.
    /// </summary>
    /// <param name="key">The replay key, usually the challenge hash.</param>
    /// <param name="expiresAt">The time after which the replay key may be discarded.</param>
    /// <returns><c>true</c> when the key was stored; otherwise <c>false</c> for a replay or expired key.</returns>
    bool TryStoreOnce(string key, DateTimeOffset expiresAt);
}
