using System.Text.Json;
using System.Text.Json.Serialization;

namespace Altcha.Net;

public sealed class AltchaChallenge
{
    public AltchaChallenge(string algorithm, string challenge, string salt, string signature, int maxNumber)
    {
        Algorithm = algorithm;
        Challenge = challenge;
        Salt = salt;
        Signature = signature;
        MaxNumber = maxNumber;
    }

    [JsonPropertyName("algorithm")]
    public string Algorithm { get; }

    [JsonPropertyName("challenge")]
    public string Challenge { get; }

    [JsonPropertyName("salt")]
    public string Salt { get; }

    [JsonPropertyName("signature")]
    public string Signature { get; }

    [JsonPropertyName("maxnumber")]
    public int MaxNumber { get; }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, AltchaJson.Options);
    }
}
