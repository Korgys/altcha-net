using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Altcha.Net;

public sealed class AltchaService
{
    private readonly AltchaOptions _options;
    private readonly IAltchaReplayStore _replayStore;

    public AltchaService(AltchaOptions options)
        : this(options, new MemoryAltchaReplayStore())
    {
    }

    public AltchaService(AltchaOptions options, IAltchaReplayStore replayStore)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _replayStore = replayStore ?? throw new ArgumentNullException(nameof(replayStore));
    }

    public AltchaChallenge GenerateChallenge()
    {
        var expires = DateTimeOffset.UtcNow.Add(_options.ChallengeExpiry).ToUnixTimeSeconds();
        var salt = AltchaCrypto.RandomHex(_options.SaltLength) + "?expires=" + expires.ToString(CultureInfo.InvariantCulture) + "&";
        var number = AltchaCrypto.RandomInt(_options.Complexity.MinNumber, _options.Complexity.MaxNumber);
        var challenge = AltchaCrypto.HashHex(_options.Algorithm, salt + number.ToString(CultureInfo.InvariantCulture));
        var signature = AltchaCrypto.HmacHex(_options.Algorithm, challenge, _options.SecretKey);

        return new AltchaChallenge(_options.Algorithm, challenge, salt, signature, _options.Complexity.MaxNumber);
    }

    public AltchaValidationResult ValidateResponse(string? altchaFormValue)
    {
        if (string.IsNullOrWhiteSpace(altchaFormValue))
        {
            return AltchaValidationResult.Failure(AltchaValidationError.MissingPayload);
        }

        if (!TryDecodePayload(altchaFormValue!, out var json))
        {
            return AltchaValidationResult.Failure(AltchaValidationError.InvalidBase64);
        }

        AltchaPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<AltchaPayload>(json, AltchaJson.Options);
        }
        catch (JsonException)
        {
            return AltchaValidationResult.Failure(AltchaValidationError.InvalidJson);
        }
        catch (NotSupportedException)
        {
            return AltchaValidationResult.Failure(AltchaValidationError.InvalidJson);
        }

        if (payload == null ||
            string.IsNullOrWhiteSpace(payload.Algorithm) ||
            string.IsNullOrWhiteSpace(payload.Challenge) ||
            string.IsNullOrWhiteSpace(payload.Salt) ||
            string.IsNullOrWhiteSpace(payload.Signature) ||
            !payload.Number.HasValue)
        {
            return AltchaValidationResult.Failure(AltchaValidationError.MalformedPayload);
        }

        if (!string.Equals(payload.Algorithm, AltchaAlgorithms.Sha256, StringComparison.Ordinal))
        {
            return AltchaValidationResult.Failure(AltchaValidationError.UnsupportedAlgorithm);
        }

        if (payload.Number.Value < 0 || payload.Number.Value > _options.Complexity.MaxNumber)
        {
            return AltchaValidationResult.Failure(AltchaValidationError.InvalidNumber);
        }

        var algorithm = payload.Algorithm!;
        var challenge = payload.Challenge!;
        var number = payload.Number.Value;
        var salt = payload.Salt!;
        var signature = payload.Signature!;

        if (!salt.EndsWith("&", StringComparison.Ordinal) ||
            !TryReadExpires(salt, out var expiresAt))
        {
            return AltchaValidationResult.Failure(AltchaValidationError.InvalidChallenge);
        }

        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            return AltchaValidationResult.Failure(AltchaValidationError.Expired);
        }

        var expectedSignature = AltchaCrypto.HmacHex(algorithm, challenge, _options.SecretKey);
        if (!AltchaCrypto.FixedTimeEquals(expectedSignature, signature))
        {
            return AltchaValidationResult.Failure(AltchaValidationError.InvalidSignature);
        }

        var expectedChallenge = AltchaCrypto.HashHex(algorithm, salt + number.ToString(CultureInfo.InvariantCulture));
        if (!AltchaCrypto.FixedTimeEquals(expectedChallenge, challenge))
        {
            return AltchaValidationResult.Failure(AltchaValidationError.InvalidProofOfWork);
        }

        if (!_replayStore.TryStoreOnce(challenge, expiresAt))
        {
            return AltchaValidationResult.Failure(AltchaValidationError.ReplayDetected);
        }

        return AltchaValidationResult.Success();
    }

    private static bool TryDecodePayload(string value, out string json)
    {
        json = string.Empty;
        var normalized = value.Trim().Replace('-', '+').Replace('_', '/');
        var padding = normalized.Length % 4;
        if (padding == 2)
        {
            normalized += "==";
        }
        else if (padding == 3)
        {
            normalized += "=";
        }
        else if (padding != 0)
        {
            return false;
        }

        try
        {
            json = Encoding.UTF8.GetString(Convert.FromBase64String(normalized));
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool TryReadExpires(string salt, out DateTimeOffset expiresAt)
    {
        expiresAt = default;
        var queryStart = salt.IndexOf('?');
        if (queryStart < 0 || queryStart == salt.Length - 1)
        {
            return false;
        }

        var query = salt.Substring(queryStart + 1);
        foreach (var pair in query.Split('&'))
        {
            if (string.IsNullOrEmpty(pair))
            {
                continue;
            }

            var separator = pair.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = Uri.UnescapeDataString(pair.Substring(0, separator));
            if (!string.Equals(key, "expires", StringComparison.Ordinal) &&
                !string.Equals(key, "expire", StringComparison.Ordinal))
            {
                continue;
            }

            var value = Uri.UnescapeDataString(pair.Substring(separator + 1));
            if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unixSeconds))
            {
                return false;
            }

            try
            {
                expiresAt = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        return false;
    }
}
