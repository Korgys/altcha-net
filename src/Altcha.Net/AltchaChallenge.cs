#if !NET48
using System.Text.Json.Serialization;
#endif

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

#if !NET48
    [JsonPropertyName("algorithm")]
#endif
    public string Algorithm { get; }

#if !NET48
    [JsonPropertyName("challenge")]
#endif
    public string Challenge { get; }

#if !NET48
    [JsonPropertyName("salt")]
#endif
    public string Salt { get; }

#if !NET48
    [JsonPropertyName("signature")]
#endif
    public string Signature { get; }

#if !NET48
    [JsonPropertyName("maxnumber")]
#endif
    public int MaxNumber { get; }

    public string ToJson()
    {
        return AltchaJson.SerializeChallenge(this);
    }
}
