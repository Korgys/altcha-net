using System.Text.Json;
using System.Text.Json.Serialization;

namespace Altcha.Net;

internal static class AltchaJson
{
    public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };
}
