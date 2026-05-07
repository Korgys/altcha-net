namespace Altcha.Net.AspNetCore;

/// <summary>
/// Provides a strictly atomic "store if absent" operation for replay keys.
/// </summary>
public interface IAtomicAltchaReplayStore
{
    /// <summary>
    /// Tries to store the replay key once in an atomic way.
    /// </summary>
    /// <returns><c>true</c> when the key was inserted; otherwise <c>false</c>.</returns>
    bool TryStoreOnceAtomic(string key, DateTimeOffset expiresAt);
}
