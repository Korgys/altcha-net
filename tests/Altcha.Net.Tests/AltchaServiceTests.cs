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

    private static AltchaService CreateService()
    {
        return new AltchaService(new AltchaOptions
        {
            SecretKey = Secret,
            ChallengeExpiry = TimeSpan.FromMinutes(2),
            Complexity = new AltchaComplexity(0, 5)
        }, new MemoryAltchaReplayStore());
    }

    private static string CreateSolvedPayload(AltchaChallenge challenge, string? signature = null)
    {
        for (var number = 0; number <= challenge.MaxNumber; number++)
        {
            if (string.Equals(Sha256Hex(challenge.Salt + number), challenge.Challenge, StringComparison.Ordinal))
            {
                return EncodePayload(challenge.Algorithm, challenge.Challenge, number, challenge.Salt, signature ?? challenge.Signature);
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
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private static string HmacSha256Hex(string value, string secret)
    {
        return Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }
}
