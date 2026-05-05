using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Altcha.Net;

internal static class AltchaCrypto
{
    public static string RandomHex(int byteLength)
    {
        var bytes = new byte[byteLength];
        using (var random = RandomNumberGenerator.Create())
        {
            random.GetBytes(bytes);
        }

        return ToHex(bytes);
    }

    public static int RandomInt(int minInclusive, int maxInclusive)
    {
        if (maxInclusive < minInclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(maxInclusive));
        }

        var range = (uint)((long)maxInclusive - minInclusive + 1);
        var limit = uint.MaxValue - (uint.MaxValue % range);
        var bytes = new byte[4];

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
    }

    public static string HashHex(string algorithm, string value)
    {
        if (!string.Equals(algorithm, AltchaAlgorithms.Sha256, StringComparison.Ordinal))
        {
            throw new NotSupportedException("Only SHA-256 is supported by this MVP.");
        }

        using (var sha = SHA256.Create())
        {
            return ToHex(sha.ComputeHash(Encoding.UTF8.GetBytes(value)));
        }
    }

    public static string HmacHex(string algorithm, string value, string secret)
    {
        if (!string.Equals(algorithm, AltchaAlgorithms.Sha256, StringComparison.Ordinal))
        {
            throw new NotSupportedException("Only SHA-256 is supported by this MVP.");
        }

        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            return ToHex(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
        }
    }

    public static bool FixedTimeEquals(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        var difference = 0;
        for (var i = 0; i < left.Length; i++)
        {
            difference |= left[i] ^ right[i];
        }

        return difference == 0;
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
