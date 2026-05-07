namespace Altcha.Net;

public interface IAltchaReplayStoreAsync
{
    ValueTask<bool> TryStoreOnceAsync(string key, DateTimeOffset expiresAt, CancellationToken ct = default);
}
