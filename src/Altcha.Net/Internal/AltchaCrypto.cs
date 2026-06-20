using System.Globalization;
#if NET10_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
using System.Security.Cryptography;
using System.Text;

namespace Altcha.Net;

internal static class AltchaCrypto
{
    public static string RandomHex(int byteLength)
    {
        var bytes = new byte[byteLength];
#if NET10_0_OR_GREATER
        RandomNumberGenerator.Fill(bytes);
#else
        using (var random = RandomNumberGenerator.Create())
        {
            random.GetBytes(bytes);
        }
#endif

        return ToHex(bytes);
    }

    public static int RandomInt(int minInclusive, int maxInclusive)
    {
        if (maxInclusive < minInclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(maxInclusive));
        }

#if NET10_0_OR_GREATER
        if (maxInclusive < int.MaxValue)
        {
            return RandomNumberGenerator.GetInt32(minInclusive, maxInclusive + 1);
        }
#endif

        var range = (uint)((long)maxInclusive - minInclusive + 1);
        var limit = uint.MaxValue - (uint.MaxValue % range);
        var bytes = new byte[4];

#if NET10_0_OR_GREATER
        while (true)
        {
            RandomNumberGenerator.Fill(bytes);
            var value = BitConverter.ToUInt32(bytes, 0);
            if (value < limit)
            {
                return minInclusive + (int)(value % range);
            }
        }
#else
        using (var random = RandomNumberGenerator.Create())
        {
            while (true)
            {
                random.GetBytes(bytes);
                var value = BitConverter.ToUInt32(bytes, 0);
                if (value < limit)
                {
                    return minInclusive + (int)(value % range);
                }
            }
        }
#endif
    }

    public static string HashHex(string algorithm, string value)
    {
        EnsureSha256(algorithm);

        using (var sha = SHA256.Create())
        {
            return ToHex(sha.ComputeHash(Encoding.UTF8.GetBytes(value)));
        }
    }

    public static string HmacHex(string algorithm, string value, string secret)
    {
        EnsureSha256(algorithm);
        return HmacSha256Hex(value, Encoding.UTF8.GetBytes(secret));
    }

    public static string HmacHex(string algorithm, string value, byte[] secretBytes)
    {
        EnsureSha256(algorithm);
        return HmacSha256Hex(value, secretBytes);
    }

    public static bool FixedTimeEquals(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

#if NET10_0_OR_GREATER
        return CryptographicOperations.FixedTimeEquals(
            MemoryMarshal.AsBytes(left.AsSpan()),
            MemoryMarshal.AsBytes(right.AsSpan()));
#else
        var difference = 0;
        for (var i = 0; i < left.Length; i++)
        {
            difference |= left[i] ^ right[i];
        }

        return difference == 0;
#endif
    }

    private static void EnsureSha256(string algorithm)
    {
        if (!string.Equals(algorithm, AltchaAlgorithms.Sha256, StringComparison.Ordinal))
        {
            throw new NotSupportedException("Only SHA-256 is currently supported.");
        }
    }

    private static string HmacSha256Hex(string value, byte[] secretBytes)
    {
        using (var hmac = new HMACSHA256(secretBytes))
        {
            return ToHex(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
        }
    }

    private static string ToHex(byte[] bytes)
    {
        var chars = new char[bytes.Length * 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            chars[i * 2] = GetHexChar(b >> 4);
            chars[(i * 2) + 1] = GetHexChar(b & 0xF);
        }

        return new string(chars);
    }

    private static char GetHexChar(int value)
    {
        return value < 10 ? (char)('0' + value) : (char)('a' + value - 10);
    }
}
