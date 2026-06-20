using System.Security.Cryptography;
using System.Text;
#if NET48
using System.Web.Script.Serialization;
#else
using System.Text.Json;
#endif

namespace Altcha.Net.Tests;

public sealed class AltchaServiceTests
{
    private const string Secret = "unit-test-secret";
    private const string InteropSecret = "interop-secret";
    private const int InteropNumber = 3;
    private const string InteropSalt = "interop-saltx?expires=4102444800&";
    private const string InteropChallenge = "4d22a70265954f5ab4958921ecade4ad1c133db77ba225c645629ad3995338f6";
    private const string InteropSignature = "87b3b55c7f36cf4176bccaa2e7b6b8364252a6188740ffb880619cc75c08d99b";

    [Fact]
    public void HashHex_ReturnsExpectedSha256()
    {
        var hash = AltchaCrypto.HashHex("SHA-256", InteropSalt + InteropNumber.ToString());

        Assert.Equal(InteropChallenge, hash);
    }

    [Fact]
    public void HmacHex_ReturnsExpectedSha256Signature()
    {
        var signature = AltchaCrypto.HmacHex("SHA-256", InteropChallenge, Encoding.UTF8.GetBytes(InteropSecret));

        Assert.Equal(InteropSignature, signature);
        Assert.Equal(AltchaCrypto.HmacHex("SHA-256", InteropChallenge, InteropSecret), signature);
    }

    [Fact]
    public void RandomInt_ReturnsValuesWithinInclusiveBounds()
    {
        for (var i = 0; i < 256; i++)
        {
            var value = AltchaCrypto.RandomInt(2, 4);

            Assert.InRange(value, 2, 4);
        }
    }

    [Fact]
    public void FixedTimeEquals_ReturnsExpectedComparisonResult()
    {
        Assert.True(AltchaCrypto.FixedTimeEquals(InteropChallenge, InteropChallenge));
        Assert.False(AltchaCrypto.FixedTimeEquals(InteropChallenge, "0" + InteropChallenge.Substring(1)));
    }

    [Fact]
    public void GenerateChallenge_ReturnsWidgetCompatibleChallenge()
    {
        var service = CreateService();

        var challenge = service.GenerateChallenge();
        var root = ParseJsonObject(challenge.ToJson());

        Assert.Equal("SHA-256", challenge.Algorithm);
        Assert.Equal(64, challenge.Challenge.Length);
        Assert.Equal(64, challenge.Signature.Length);
        Assert.Equal(5, challenge.MaxNumber);
        Assert.Contains("?expires=", challenge.Salt);
        Assert.EndsWith("&", challenge.Salt);
        Assert.Equal(5, root.Count);
        Assert.Equal(challenge.Algorithm, ReadString(root, "algorithm"));
        Assert.Equal(challenge.Challenge, ReadString(root, "challenge"));
        Assert.Equal(challenge.MaxNumber, ReadInt32(root, "maxnumber"));
        Assert.Equal(challenge.Salt, ReadString(root, "salt"));
        Assert.Equal(challenge.Signature, ReadString(root, "signature"));
        Assert.False(root.ContainsKey("number"));
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ValidateResponse_AcceptsFixedInteropPayloads(bool urlSafe)
    {
        var service = CreateInteropService();
        var json = CreateInteropJson(InteropNumber.ToString());
        var payload = urlSafe
            ? EncodeJsonAsUrlSafeBase64WithoutPadding(json)
            : EncodeJsonAsBase64(json);

        var result = service.ValidateResponse(payload);

        if (urlSafe)
        {
            Assert.DoesNotContain("=", payload);
        }
        else
        {
            Assert.EndsWith("==", payload);
        }

        Assert.True(result.IsValid);
        Assert.Equal(AltchaValidationError.None, result.Error);
    }

    [Fact]
    public void ValidateResponse_AcceptsFixedInteropPayloadWithNumberAsJsonString()
    {
        var service = CreateInteropService();
        var payload = EncodeJsonAsBase64(CreateInteropJson("\"3\""));

        var result = service.ValidateResponse(payload);

        Assert.True(result.IsValid);
        Assert.Equal(AltchaValidationError.None, result.Error);
    }

    [Fact]
    public void ValidateResponse_AcceptsExpireSaltAlias()
    {
        var clock = new FakeClock(new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero));
        var service = CreateService(clock: clock);
        var salt = "abcdef?expire=" + clock.UtcNow.AddMinutes(1).ToUnixTimeSeconds() + "&";
        var challengeHash = Sha256Hex(salt + "1");
        var signature = HmacSha256Hex(challengeHash, Secret);
        var payload = EncodePayload("SHA-256", challengeHash, 1, salt, signature);

        var result = service.ValidateResponse(payload);

        Assert.True(result.IsValid);
        Assert.Equal(AltchaValidationError.None, result.Error);
    }

    [Fact]
    public void ValidateResponse_AcceptsUrlEncodedExpiresKeyInSalt()
    {
        var service = CreateService(clock: new FakeClock(new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero)));
        var salt = "abcdef?exp%69res=4102444800&";
        var payload = CreateSolvedPayload(salt, 1);

        var result = service.ValidateResponse(payload);

        Assert.True(result.IsValid);
        Assert.Equal(AltchaValidationError.None, result.Error);
    }

    [Fact]
    public void GenerateChallenge_UsesClockForExpiresTimestamp()
    {
        var clock = new FakeClock(new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero));
        var service = CreateService(clock: clock);
        var expectedExpires = clock.UtcNow.AddMinutes(2).ToUnixTimeSeconds();

        var challenge = service.GenerateChallenge();

        Assert.Contains("?expires=" + expectedExpires + "&", challenge.Salt);
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
    public void ValidateResponse_RejectsFixedInteropPayloadWithInvalidSignature()
    {
        var service = CreateInteropService();
        var payload = EncodeJsonAsBase64(CreateInteropJson(
            InteropNumber.ToString(),
            signature: "0" + InteropSignature.Substring(1)));

        var result = service.ValidateResponse(payload);

        Assert.False(result.IsValid);
        Assert.Equal(AltchaValidationError.InvalidSignature, result.Error);
    }

    [Fact]
    public void ValidateResponse_RejectsMalformedNumberString()
    {
        var service = CreateInteropService();
        var payload = EncodeJsonAsBase64(CreateInteropJson("\"not-a-number\""));

        var result = service.ValidateResponse(payload);

        Assert.False(result.IsValid);
#if NET48
        Assert.Equal(AltchaValidationError.MalformedPayload, result.Error);
#else
        Assert.Equal(AltchaValidationError.InvalidJson, result.Error);
#endif
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
        var clock = new FakeClock(new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero));
        var service = CreateService(clock: clock);
        var salt = "abcdef?expires=" + clock.UtcNow.AddMinutes(-1).ToUnixTimeSeconds() + "&";
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
        var expiresAt = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var clock = new FakeClock(expiresAt.AddSeconds(5).AddTicks(-1));
        var service = CreateService(new AltchaOptions
        {
            SecretKey = Secret,
            ChallengeExpiry = TimeSpan.FromMinutes(2),
            AllowedClockSkew = TimeSpan.FromSeconds(5),
            Complexity = new AltchaComplexity(0, 5)
        }, clock);
        var salt = "abcdef?expires=" + expiresAt.ToUnixTimeSeconds() + "&";
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
        var expiresAt = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var clock = new FakeClock(expiresAt.AddSeconds(5));
        var service = CreateService(new AltchaOptions
        {
            SecretKey = Secret,
            ChallengeExpiry = TimeSpan.FromMinutes(2),
            AllowedClockSkew = TimeSpan.FromSeconds(5),
            Complexity = new AltchaComplexity(0, 5)
        }, clock);
        var salt = "abcdef?expires=" + expiresAt.ToUnixTimeSeconds() + "&";
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
        var clock = new FakeClock(new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero));
        var service = CreateService(clock: clock);
        var salt = "abcdef?expires=" + clock.UtcNow.AddMinutes(1).ToUnixTimeSeconds() + "&";
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
        var clock = new FakeClock(new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero));
        var service = CreateService(clock: clock);
        var salt = "abcdef?expires=" + clock.UtcNow.AddMinutes(1).ToUnixTimeSeconds() + "&";
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
    public void ValidateResponse_RejectsSaltWithoutTrailingAmpersand()
    {
        var service = CreateService(clock: new FakeClock(new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero)));
        var payload = CreateSolvedPayload("abcdef?expires=4102444800", 1);

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
    public void ValidateResponse_RejectsPayloadTooLarge()
    {
        var service = CreateService(new AltchaOptions
        {
            SecretKey = Secret,
            MaxPayloadLength = 256,
            Complexity = new AltchaComplexity(0, 5)
        });
        var payload = new string('!', 257);

        var result = service.ValidateResponse(payload);

        Assert.False(result.IsValid);
        Assert.Equal(AltchaValidationError.PayloadTooLarge, result.Error);
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

    [Theory]
    [InlineData(255)]
    [InlineData(65537)]
    public void Constructor_RejectsInvalidMaxPayloadLength(int maxPayloadLength)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new AltchaService(new AltchaOptions
        {
            SecretKey = Secret,
            MaxPayloadLength = maxPayloadLength
        }));

        Assert.Equal("MaxPayloadLength", exception.ParamName);
    }

    private static AltchaService CreateService(AltchaOptions? options = null, IAltchaClock? clock = null)
    {
        options ??= new AltchaOptions
        {
            SecretKey = Secret,
            ChallengeExpiry = TimeSpan.FromMinutes(2),
            Complexity = new AltchaComplexity(0, 5)
        };

        return clock == null
            ? new AltchaService(options, new MemoryAltchaReplayStore())
            : new AltchaService(options, new MemoryAltchaReplayStore(), clock);
    }

    private static AltchaService CreateInteropService()
    {
        return CreateService(new AltchaOptions
        {
            SecretKey = InteropSecret,
            ChallengeExpiry = TimeSpan.FromMinutes(2),
            Complexity = new AltchaComplexity(0, 5)
        }, new FakeClock(new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero)));
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

    private static string CreateSolvedPayload(string salt, int number)
    {
        var challengeHash = Sha256Hex(salt + number);
        var signature = HmacSha256Hex(challengeHash, Secret);
        return EncodePayload("SHA-256", challengeHash, number, salt, signature);
    }

    private static string CreateInteropJson(string numberJson, string? signature = null)
    {
        return "{\"algorithm\":\"SHA-256\",\"challenge\":\"" + InteropChallenge +
            "\",\"number\":" + numberJson +
            ",\"salt\":\"" + InteropSalt +
            "\",\"signature\":\"" + (signature ?? InteropSignature) + "\"}";
    }

    private static string EncodePayload(string algorithm, string challenge, int number, string salt, string signature)
    {
        var json = SerializeJson(new Dictionary<string, object?>
        {
            ["algorithm"] = algorithm,
            ["challenge"] = challenge,
            ["number"] = number,
            ["salt"] = salt,
            ["signature"] = signature
        });

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private static string EncodeJsonAsBase64(string json)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private static string EncodeJsonAsUrlSafeBase64WithoutPadding(string json)
    {
        return EncodeJsonAsBase64(json)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static Dictionary<string, object?> ParseJsonObject(string json)
    {
#if NET48
        var values = new JavaScriptSerializer().DeserializeObject(json) as Dictionary<string, object>;
        return values?.ToDictionary(item => item.Key, item => (object?)item.Value)
            ?? new Dictionary<string, object?>();
#else
        using var document = JsonDocument.Parse(json);
        return document.RootElement.EnumerateObject().ToDictionary(
            item => item.Name,
            item => item.Value.ValueKind == JsonValueKind.Number
                ? (object?)item.Value.GetInt32()
                : item.Value.GetString());
#endif
    }

    private static string SerializeJson(Dictionary<string, object?> values)
    {
#if NET48
        return new JavaScriptSerializer().Serialize(values);
#else
        return JsonSerializer.Serialize(values);
#endif
    }

    private static string? ReadString(Dictionary<string, object?> values, string key)
    {
        return values.TryGetValue(key, out var value) ? Convert.ToString(value) : null;
    }

    private static int ReadInt32(Dictionary<string, object?> values, string key)
    {
        return values.TryGetValue(key, out var value) ? Convert.ToInt32(value) : 0;
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

    private sealed class FakeClock : IAltchaClock
    {
        public FakeClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; set; }
    }
}
