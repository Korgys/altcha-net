#if NET48
using System.Collections.Generic;
using System.Globalization;
using System.Web.Script.Serialization;
#else
using System.Text.Json;
using System.Text.Json.Serialization;
#endif

namespace Altcha.Net;

internal static class AltchaJson
{
#if !NET48
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };
#endif

    public static string SerializeChallenge(AltchaChallenge challenge)
    {
#if NET48
        return new JavaScriptSerializer().Serialize(new Dictionary<string, object>
        {
            ["algorithm"] = challenge.Algorithm,
            ["challenge"] = challenge.Challenge,
            ["salt"] = challenge.Salt,
            ["signature"] = challenge.Signature,
            ["maxnumber"] = challenge.MaxNumber
        });
#else
        return JsonSerializer.Serialize(challenge, Options);
#endif
    }

    public static bool TryDeserializePayload(string json, out AltchaPayload? payload)
    {
        payload = null;

        try
        {
#if NET48
            var values = new JavaScriptSerializer().DeserializeObject(json) as IDictionary<string, object>;
            if (values == null)
            {
                return false;
            }

            payload = new AltchaPayload
            {
                Algorithm = ReadString(values, "algorithm"),
                Challenge = ReadString(values, "challenge"),
                Number = ReadInt32(values, "number"),
                Salt = ReadString(values, "salt"),
                Signature = ReadString(values, "signature")
            };
#else
            payload = JsonSerializer.Deserialize<AltchaPayload>(json, Options);
#endif
            return true;
        }
#if NET48
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
#else
        catch (JsonException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
#endif
    }

#if NET48
    private static string? ReadString(IDictionary<string, object> values, string key)
    {
        return TryGetValue(values, key, out var value) && value != null
            ? Convert.ToString(value, CultureInfo.InvariantCulture)
            : null;
    }

    private static int? ReadInt32(IDictionary<string, object> values, string key)
    {
        if (!TryGetValue(values, key, out var value) || value == null)
        {
            return null;
        }

        if (value is int intValue)
        {
            return intValue;
        }

        if (value is long longValue && longValue >= int.MinValue && longValue <= int.MaxValue)
        {
            return (int)longValue;
        }

        if (value is decimal decimalValue &&
            decimal.Truncate(decimalValue) == decimalValue &&
            decimalValue >= int.MinValue &&
            decimalValue <= int.MaxValue)
        {
            return (int)decimalValue;
        }

        if (int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static bool TryGetValue(IDictionary<string, object> values, string key, out object? value)
    {
        foreach (var item in values)
        {
            if (string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = item.Value;
                return true;
            }
        }

        value = null;
        return false;
    }
#endif
}
