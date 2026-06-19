namespace Altcha.Net;

internal sealed class SystemAltchaClock : IAltchaClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
