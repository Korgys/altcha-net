#if !NET48
using System.Text.Json.Serialization;
#endif

namespace Altcha.Net;

internal sealed class AltchaPayload
{
#if !NET48
    [JsonPropertyName("algorithm")]
#endif
    public string? Algorithm { get; set; }

#if !NET48
    [JsonPropertyName("challenge")]
#endif
    public string? Challenge { get; set; }

#if !NET48
    [JsonPropertyName("number")]
#endif
    public int? Number { get; set; }

#if !NET48
    [JsonPropertyName("salt")]
#endif
    public string? Salt { get; set; }

#if !NET48
    [JsonPropertyName("signature")]
#endif
    public string? Signature { get; set; }
}
