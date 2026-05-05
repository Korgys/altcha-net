namespace Altcha.Net;

public enum AltchaValidationError
{
    None = 0,
    MissingPayload,
    InvalidBase64,
    InvalidJson,
    MalformedPayload,
    UnsupportedAlgorithm,
    InvalidNumber,
    InvalidChallenge,
    Expired,
    InvalidSignature,
    InvalidProofOfWork,
    ReplayDetected
}
