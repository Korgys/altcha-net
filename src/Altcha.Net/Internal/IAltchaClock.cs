namespace Altcha.Net;

internal interface IAltchaClock
{
    DateTimeOffset UtcNow { get; }
}
