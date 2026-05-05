namespace Altcha.Net;

public interface IAltchaReplayStore
{
    bool TryStoreOnce(string key, DateTimeOffset expiresAt);
}
