namespace Altcha.Net;

/// <summary>
/// Identifies why an ALTCHA validation attempt failed.
/// </summary>
public enum AltchaValidationError
{
    /// <summary>
    /// Validation succeeded.
    /// </summary>
    None = 0,

    /// <summary>
    /// The submitted ALTCHA payload was missing.
    /// </summary>
    MissingPayload,

    /// <summary>
    /// The submitted ALTCHA payload was not valid Base64.
    /// </summary>
    InvalidBase64,

    /// <summary>
    /// The decoded ALTCHA payload was not valid JSON.
    /// </summary>
    InvalidJson,

    /// <summary>
    /// The decoded ALTCHA payload did not contain the required fields.
    /// </summary>
    MalformedPayload,

    /// <summary>
    /// The payload requested an unsupported hashing algorithm.
    /// </summary>
    UnsupportedAlgorithm,

    /// <summary>
    /// The proof-of-work number was outside the configured range.
    /// </summary>
    InvalidNumber,

    /// <summary>
    /// The challenge metadata was missing or invalid.
    /// </summary>
    InvalidChallenge,

    /// <summary>
    /// The challenge expired before validation.
    /// </summary>
    Expired,

    /// <summary>
    /// The challenge signature did not match the server secret.
    /// </summary>
    InvalidSignature,

    /// <summary>
    /// The submitted proof of work did not solve the challenge.
    /// </summary>
    InvalidProofOfWork,

    /// <summary>
    /// The challenge was already used.
    /// </summary>
    ReplayDetected,

    /// <summary>
    /// The submitted payload exceeded the configured maximum length.
    /// </summary>
    PayloadTooLarge
}
