#if !NET48
using System.Text.Json.Serialization;
#endif

namespace Altcha.Net;

/// <summary>
/// Represents a challenge returned to the client-side ALTCHA widget.
/// </summary>
public sealed class AltchaChallenge
{
    /// <summary>
    /// Creates an ALTCHA challenge payload.
    /// </summary>
    /// <param name="algorithm">The hashing algorithm clients must use.</param>
    /// <param name="challenge">The target hash clients must solve.</param>
    /// <param name="salt">The challenge salt, including server-side metadata such as expiry.</param>
    /// <param name="signature">The server signature used to validate the challenge later.</param>
    /// <param name="maxNumber">The maximum proof-of-work number clients should try.</param>
    public AltchaChallenge(string algorithm, string challenge, string salt, string signature, int maxNumber)
    {
        Algorithm = algorithm;
        Challenge = challenge;
        Salt = salt;
        Signature = signature;
        MaxNumber = maxNumber;
    }

    /// <summary>
    /// Gets the hashing algorithm clients must use.
    /// </summary>
#if !NET48
    [JsonPropertyName("algorithm")]
#endif
    public string Algorithm { get; }

    /// <summary>
    /// Gets the target hash clients must solve.
    /// </summary>
#if !NET48
    [JsonPropertyName("challenge")]
#endif
    public string Challenge { get; }

    /// <summary>
    /// Gets the challenge salt, including server-side metadata such as expiry.
    /// </summary>
#if !NET48
    [JsonPropertyName("salt")]
#endif
    public string Salt { get; }

    /// <summary>
    /// Gets the server signature used to validate the challenge later.
    /// </summary>
#if !NET48
    [JsonPropertyName("signature")]
#endif
    public string Signature { get; }

    /// <summary>
    /// Gets the maximum proof-of-work number clients should try.
    /// </summary>
#if !NET48
    [JsonPropertyName("maxnumber")]
#endif
    public int MaxNumber { get; }

    /// <summary>
    /// Serializes the challenge using the JSON field names expected by ALTCHA clients.
    /// </summary>
    /// <returns>The JSON representation of the challenge.</returns>
    public string ToJson()
    {
        return AltchaJson.SerializeChallenge(this);
    }
}
