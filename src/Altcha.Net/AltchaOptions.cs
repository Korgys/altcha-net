namespace Altcha.Net;

public sealed class AltchaOptions
{
    private const int MinPayloadLength = 256;
    private const int MaxPayloadLengthLimit = 65536;

    public string SecretKey { get; set; } = string.Empty;

    public TimeSpan ChallengeExpiry { get; set; } = TimeSpan.FromMinutes(2);

    public TimeSpan AllowedClockSkew { get; set; } = TimeSpan.FromSeconds(10);

    public AltchaComplexity Complexity { get; set; } = new AltchaComplexity(50000, 100000);

    public string Algorithm { get; set; } = AltchaAlgorithms.Sha256;

    public int SaltLength { get; set; } = 12;

    public int MaxPayloadLength { get; set; } = 4096;

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(SecretKey))
        {
            throw new ArgumentException("The ALTCHA secret key is required.", nameof(SecretKey));
        }

        if (ChallengeExpiry <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ChallengeExpiry), "The challenge expiry must be greater than zero.");
        }

        if (AllowedClockSkew < TimeSpan.Zero || AllowedClockSkew > TimeSpan.FromMinutes(1))
        {
            throw new ArgumentOutOfRangeException(nameof(AllowedClockSkew), "The allowed clock skew must be between 0 and 1 minute.");
        }

        if (!string.Equals(Algorithm, AltchaAlgorithms.Sha256, StringComparison.Ordinal))
        {
            throw new NotSupportedException("Only SHA-256 is currently supported.");
        }

        if (Complexity == null)
        {
            throw new ArgumentNullException(nameof(Complexity));
        }

        if (SaltLength < 8 || SaltLength > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(SaltLength), "The salt length must be between 8 and 64 bytes.");
        }

        if (MaxPayloadLength < MinPayloadLength || MaxPayloadLength > MaxPayloadLengthLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxPayloadLength), "The maximum payload length must be between 256 and 65536 characters.");
        }
    }
}
