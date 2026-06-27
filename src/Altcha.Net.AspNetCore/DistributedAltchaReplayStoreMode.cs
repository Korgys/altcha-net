namespace Altcha.Net.AspNetCore;

/// <summary>
/// Selects how distributed replay protection handles concurrency.
/// </summary>
public enum DistributedAltchaReplayStoreMode
{
    /// <summary>
    /// Uses generic distributed cache operations; this is not strictly atomic across concurrent workers.
    /// </summary>
    BestEffort = 0,

    /// <summary>
    /// Requires an atomic replay store so challenge keys are inserted only once across workers.
    /// </summary>
    StrictAtomic = 1
}
