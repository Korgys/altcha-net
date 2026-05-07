using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Altcha.Net.Tests;

public sealed class AltchaServiceTests
{
    private const string Secret = "unit-test-secret";

    [Fact]
    public void GenerateChallenge_ReturnsWidgetCompatibleChallenge()
    {
        var service = CreateService();

        var challenge = service.GenerateChallenge();
        using var json = JsonDocument.Parse(challenge.ToJson());
        var root = json.RootElement;

        Assert.Equal("SHA-256", challenge.Algorithm);
        Assert.Equal(64, challenge.Challenge.Length);
        Assert.Equal(64, challenge.Signature.Length);
        Assert.Equal(5, challenge.MaxNumber);
        Assert.Contains("?expires=", challenge.Salt);
        Assert.EndsWith("&", challenge.Salt);
        Assert.Equal(5, root.EnumerateObject().Count());
        Assert.Equal(challenge.Algorithm, root.GetProperty("algorithm").GetString());
        Assert.Equal(challenge.Challenge, root.GetProperty("challenge").GetString());
        Assert.Equal(challenge.MaxNumber, root.GetProperty("maxnumber").GetInt32());
        Assert.Equal(challenge.Salt, root.GetProperty("salt").GetString());
        Assert.Equal(challenge.Signature, root.GetProperty("signature").GetString());
        Assert.False(root.TryGetProperty("number", out _));
    }

    [Fact]
    public void ValidateResponse_AcceptsWidgetLikeBase64JsonPayload()
    {
        var service = CreateService();
        var challenge = service.GenerateChallenge();
        var payload = CreateSolvedPayload(challenge);

        var result = service.ValidateResponse(payload);

        Assert.True(result.IsValid);
        Assert.Equal(AltchaValidationError.None, result.Error);
    }

    [Fact]
    public void ValidateResponse_AcceptsUrlSafeBase64PayloadWithoutPadding()
    {
        var service = CreateService();
        var challenge = service.GenerateChallenge();
        var payload = CreateSolvedPayload(challenge)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var result = service.ValidateResponse(payload);

        Assert.True(result.IsValid);
        Assert.Equal(AltchaValidationError.None, result.Error);
    }

    [Fact]
    public void ValidateResponse_AcceptsExpireSaltAlias()
    {
        var service = CreateService();
        var salt = "abcdef?expire=" + DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds() + "&";
        var challengeHash = Sha256Hex(salt + "1");
        var signature = HmacSha256Hex(challengeHash, Secret);
        var payload = EncodePayload("SHA-256", challengeHash, 1, salt, signature);

        var result = service.ValidateResponse(payload);

        Assert.True(result.IsValid);
        Assert.Equal(AltchaValidationError.None, result.Error);
    }

    [Fact]
    public void ValidateResponse_RejectsInvalidSignature()
    {
        var service = CreateService();
        var challenge = service.GenerateChallenge();
        var invalidSignaturePrefix = challenge.Signature[0] == '0' ? "1" : "0";
        var payload = CreateSolvedPayload(challenge, signature: invalidSignaturePrefix + challenge.Signature.Substring(1));

        var result = service.ValidateResponse(payload);

        Assert.False(result.IsValid);
        Assert.Equal(AltchaValidationError.InvalidSignature, result.Error);
    }

    [Fact]
    public void ValidateResponse_RejectsAlteredChallenge()
    {
        var service = CreateService();
        var challenge = service.GenerateChallenge();
        var alteredChallengePrefix = challenge.Challenge[0] == '0' ? "1" : "0";
        var alteredChallenge = alteredChallengePrefix + challenge.Challenge.Substring(1);
        var signature = HmacSha256Hex(alteredChallenge, Secret);
        var payload = EncodePayload(challenge.Algorithm, alteredChallenge, 0, challenge.Salt, signature);

        var result = service.ValidateResponse(payload);

        Assert.False(result.IsValid);
        Assert.Equal(AltchaValidationError.InvalidProofOfWork, result.Error);
    }

    [Fact]
    public void ValidateResponse_RejectsAlteredSalt()
    {
        var service = CreateService();
        var challenge = service.GenerateChallenge();
        var payload = CreateSolvedPayload(challenge, salt: "ff" + challenge.Salt);

        var result = service.ValidateResponse(payload);

        Assert.False(result.IsValid);
        Assert.Equal(AltchaValidationError.InvalidProofOfWork, result.Error);
    }

    [Fact]
    public void ValidateResponse_RejectsExpiredChallenge()
    {
        var service = CreateService();
        var salt = "abcdef?expires=" + DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds() + "&";
        var challengeHash = Sha256Hex(salt + "1");
        var signature = HmacSha256Hex(challengeHash, Secret);
        var payload = EncodePayload("SHA-256", challengeHash, 1, salt, signature);

        var result = service.ValidateResponse(payload);

        Assert.False(result.IsValid);
        Assert.Equal(AltchaValidationError.Expired, result.Error);
    }


    [Fact]
    public void ValidateResponse_AcceptsChallengeJustBeforeExpirySkewLimit()
    {
        var service = CreateService(new AltchaOptions
        {
            SecretKey = Secret,
            ChallengeExpiry = TimeSpan.FromMinutes(2),
            AllowedClockSkew = TimeSpan.FromSeconds(5),
            Complexity = new AltchaComplexity(0, 5)
        });
        var salt = "abcdef?expires=" + DateTimeOffset.UtcNow.AddSeconds(-4).ToUnixTimeSeconds() + "&";
        var challengeHash = Sha256Hex(salt + "1");
        var signature = HmacSha256Hex(challengeHash, Secret);
        var payload = EncodePayload("SHA-256", challengeHash, 1, salt, signature);

        var result = service.ValidateResponse(payload);

        Assert.True(result.IsValid);
        Assert.Equal(AltchaValidationError.None, result.Error);
    }

    [Fact]
    public void ValidateResponse_RejectsChallengeJustAfterExpirySkewLimit()
    {
        var service = CreateService(new AltchaOptions
        {
            SecretKey = Secret,
            ChallengeExpiry = TimeSpan.FromMinutes(2),
            AllowedClockSkew = TimeSpan.FromSeconds(5),
            Complexity = new AltchaComplexity(0, 5)
        });
        var salt = "abcdef?expires=" + DateTimeOffset.UtcNow.AddSeconds(-6).ToUnixTimeSeconds() + "&";
        var challengeHash = Sha256Hex(salt + "1");
        var signature = HmacSha256Hex(challengeHash, Secret);
        var payload = EncodePayload("SHA-256", challengeHash, 1, salt, signature);

        var result = service.ValidateResponse(payload);

        Assert.False(result.IsValid);
        Assert.Equal(AltchaValidationError.Expired, result.Error);
    }

    [Fact]
    public void ValidateResponse_RejectsInvalidProofOfWork()
    {
        var service = CreateService();
        var salt = "abcdef?expires=" + DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds() + "&";
        var challengeHash = Sha256Hex(salt + "1");
        var signature = HmacSha256Hex(challengeHash, Secret);
        var payload = EncodePayload("SHA-256", challengeHash, 2, salt, signature);

        var result = service.ValidateResponse(payload);

        Assert.False(result.IsValid);
        Assert.Equal(AltchaValidationError.InvalidProofOfWork, result.Error);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    public void ValidateResponse_RejectsNumberOutsideConfiguredRange(int number)
    {
        var service = CreateService();
        var salt = "abcdef?expires=" + DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds() + "&";
        var challengeHash = Sha256Hex(salt + number);
        var signature = HmacSha256Hex(challengeHash, Secret);
        var payload = EncodePayload("SHA-256", challengeHash, number, salt, signature);

        var result = service.ValidateResponse(payload);

        Assert.False(result.IsValid);
        Assert.Equal(AltchaValidationError.InvalidNumber, result.Error);
    }

    [Theory]
    [InlineData("abcdef")]
    [InlineData("abcdef?foo=bar&")]
    [InlineData("abcdef?expires=invalid&")]
    [InlineData("abcdef?expires=999999999999999999999999&")]
    public void ValidateResponse_RejectsMissingOrInvalidSaltExpires(string salt)
    {
        var service = CreateService();
        var challengeHash = Sha256Hex(salt + "1");
        var signature = HmacSha256Hex(challengeHash, Secret);
        var payload = EncodePayload("SHA-256", challengeHash, 1, salt, signature);

        var result = service.ValidateResponse(payload);

        Assert.False(result.IsValid);
        Assert.Equal(AltchaValidationError.InvalidChallenge, result.Error);
    }

    [Fact]
    public void ValidateResponse_RejectsReplay()
    {
        var service = CreateService();
        var challenge = service.GenerateChallenge();
        var payload = CreateSolvedPayload(challenge);

        var first = service.ValidateResponse(payload);
        var second = service.ValidateResponse(payload);

        Assert.True(first.IsValid);
        Assert.False(second.IsValid);
        Assert.Equal(AltchaValidationError.ReplayDetected, second.Error);
    }

    [Fact]
    public void ValidateResponse_StoresReplayAtomicallyDuringConcurrentValidation()
    {
        var service = CreateService();
        var challenge = service.GenerateChallenge();
        var payload = CreateSolvedPayload(challenge);
        var successes = 0;

        Parallel.For(0, 100, _ =>
        {
            if (service.ValidateResponse(payload).IsValid)
            {
                Interlocked.Increment(ref successes);
            }
        });

        Assert.Equal(1, successes);
    }

    [Theory]
    [InlineData("not base64", AltchaValidationError.InvalidBase64)]
    [InlineData("e2JhZA==", AltchaValidationError.InvalidJson)]
    [InlineData("e30=", AltchaValidationError.MalformedPayload)]
    public void ValidateResponse_RejectsMalformedPayload(string payload, AltchaValidationError expectedError)
    {
        var service = CreateService();

        var result = service.ValidateResponse(payload);

        Assert.False(result.IsValid);
        Assert.Equal(expectedError, result.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateResponse_RejectsMissingPayload(string? payload)
    {
        var service = CreateService();

        var result = service.ValidateResponse(payload);

        Assert.False(result.IsValid);
        Assert.Equal(AltchaValidationError.MissingPayload, result.Error);
    }

    [Fact]
    public void ValidateResponse_RejectsUnsupportedAlgorithm()
    {
        var payload = EncodePayload("SHA-512", "abc", 1, "salt?expires=9999999999&", "signature");
        var service = CreateService();

        var result = service.ValidateResponse(payload);

        Assert.False(result.IsValid);
        Assert.Equal(AltchaValidationError.UnsupportedAlgorithm, result.Error);
    }

    [Fact]
    public void MemoryReplayStore_StoresOnlyOnceUnderConcurrency()
    {
        var store = new MemoryAltchaReplayStore();
        var successes = 0;

        Parallel.For(0, 100, _ =>
        {
            if (store.TryStoreOnce("same-challenge", DateTimeOffset.UtcNow.AddMinutes(1)))
            {
                Interlocked.Increment(ref successes);
            }
        });

        Assert.Equal(1, successes);
    }

    [Fact]
    public void Constructor_RejectsMissingSecretKey()
    {
        var exception = Assert.Throws<ArgumentException>(() => new AltchaService(new AltchaOptions()));

        Assert.Equal("SecretKey", exception.ParamName);
    }

    [Fact]
    public void Constructor_RejectsInvalidChallengeExpiry()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new AltchaService(new AltchaOptions
        {
            SecretKey = Secret,
            ChallengeExpiry = TimeSpan.Zero
        }));

        Assert.Equal("ChallengeExpiry", exception.ParamName);
    }

    [Fact]
    public void Constructor_RejectsInvalidSaltLength()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new AltchaService(new AltchaOptions
        {
            SecretKey = Secret,
            SaltLength = 4
        }));

        Assert.Equal("SaltLength", exception.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(61)]
    public void Constructor_RejectsInvalidAllowedClockSkew(int seconds)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new AltchaService(new AltchaOptions
        {
            SecretKey = Secret,
            AllowedClockSkew = TimeSpan.FromSeconds(seconds)
        }));

        Assert.Equal("AllowedClockSkew", exception.ParamName);
    }

    private static AltchaService CreateService(AltchaOptions? options = null)
    {
        options ??= new AltchaOptions
        {
            SecretKey = Secret,
            ChallengeExpiry = TimeSpan.FromMinutes(2),
            Complexity = new AltchaComplexity(0, 5)
        };

        return new AltchaService(options, new MemoryAltchaReplayStore());
    }

    private static string CreateSolvedPayload(AltchaChallenge challenge, string? signature = null, string? salt = null)
    {
        for (var number = 0; number <= challenge.MaxNumber; number++)
        {
            if (string.Equals(Sha256Hex(challenge.Salt + number), challenge.Challenge, StringComparison.Ordinal))
            {
                return EncodePayload(challenge.Algorithm, challenge.Challenge, number, salt ?? challenge.Salt, signature ?? challenge.Signature);
            }
        }

        throw new InvalidOperationException("The generated challenge could not be solved in the configured range.");
    }

    private static string EncodePayload(string algorithm, string challenge, int number, string salt, string signature)
    {
        var json = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["algorithm"] = algorithm,
            ["challenge"] = challenge,
            ["number"] = number,
            ["salt"] = salt,
            ["signature"] = signature
        });

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private static string Sha256Hex(string value)
    {
        using var sha = SHA256.Create();
        return ToHex(sha.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }

    private static string HmacSha256Hex(string value, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return ToHex(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }

    private static string ToHex(byte[] bytes)
    {
        return string.Concat(bytes.Select(b => b.ToString("x2")));
    }
}
