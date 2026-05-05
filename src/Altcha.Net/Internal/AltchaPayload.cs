using System.Text.Json.Serialization;

namespace Altcha.Net;

internal sealed class AltchaPayload
{
    [JsonPropertyName("algorithm")]
    public string? Algorithm { get; set; }

    [JsonPropertyName("challenge")]
    public string? Challenge { get; set; }

    [JsonPropertyName("number")]
    public int? Number { get; set; }

    [JsonPropertyName("salt")]
    public string? Salt { get; set; }

    [JsonPropertyName("signature")]
    public string? Signature { get; set; }
}
