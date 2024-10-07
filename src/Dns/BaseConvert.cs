using System.Buffers;
using System.Text;

namespace Makaretu.Dns;

/// <summary>
/// Convert from base 16/32
/// </summary>
public static class BaseConvert
{
    /// <summary>
    /// Convert base 16 string to byte array
    /// </summary>
    /// <param name="hex"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static byte[] FromBase16(string hex)
    {
        if (hex.Length % 2 == 1)
            throw new FormatException("hex cannot have an odd number of digits");

        var arr = new byte[hex.Length / 2];
        for (var i = 0; i < hex.Length / 2; ++i)
            arr[i] = (byte)((GetHexVal(hex[i * 2]) << 4) + (GetHexVal(hex[(i * 2) + 1])));

        return arr;
    }

    /// <summary>
    /// Convert a byte array to lowercase base16
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static string ToBase16Lower(byte[] bytes)
    {
        var sb = new StringBuilder();
        foreach (var b in bytes)
            sb.Append(b.ToString("x2"));
        
        return sb.ToString();
    }

    private static int GetHexVal(int val) =>
        val is < 48 or > 57 and < 65 or > 70 and < 97 or > 102
            ? throw new FormatException("Invalid hex character")
            : val - (val < 58 ? 48 : val < 97 ? 55 : 87);

    /// <summary>
    /// Converts a byte array to a base32 hex string
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static string ToBase32Hex(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            throw new ArgumentNullException(nameof(bytes));

        var charCount = (int)Math.Ceiling(bytes.Length / 5d) * 8;
        
        var returnArray = ArrayPool<char>.Shared.Rent(charCount);

        try
        {
            byte nextChar = 0, bitsRemaining = 5;
            var arrayIndex = 0;

            foreach (var b in bytes)
            {
                nextChar = (byte)(nextChar | (b >> (8 - bitsRemaining)));
                returnArray[arrayIndex++] = ValueToChar(nextChar);

                if (bitsRemaining < 4)
                {
                    nextChar = (byte)((b >> (3 - bitsRemaining)) & 31);
                    returnArray[arrayIndex++] = ValueToChar(nextChar);
                    bitsRemaining += 5;
                }

                bitsRemaining -= 3;
                nextChar = (byte)((b << bitsRemaining) & 31);
            }

            if (arrayIndex != charCount)
            {
                returnArray[arrayIndex++] = ValueToChar(nextChar);
                //while (arrayIndex != charCount)
                //    returnArray[arrayIndex++] = '='; //padding
            }

            return string.Create(arrayIndex, (arrayIndex, returnArray), static (span, state) => state.returnArray.AsSpan(0, state.arrayIndex).CopyTo(span));
        }
        finally
        {
            ArrayPool<char>.Shared.Return(returnArray);
        }
    }

    /// <summary>
    /// Converts a base32hex string to a byte array
    /// </summary>
    /// <param name="base32"></param>
    /// <returns></returns>
    public static byte[] FromBase32Hex(string base32)
    {
        ArgumentException.ThrowIfNullOrEmpty(base32);
        
        var base32Span = base32.AsSpan().TrimEnd('=');
        var byteCount = base32Span.Length * 5 / 8;

        var returnArray = new byte[byteCount];

        byte curByte = 0, bitsRemaining = 8;
        int arrayIndex = 0;

        foreach (var c in base32Span)
        {
            var cValue = CharToValue(c);

            int mask;
            if (bitsRemaining > 5)
            {
                mask = cValue << (bitsRemaining - 5);
                curByte = (byte)(curByte | mask);
                bitsRemaining -= 5;
            }
            else
            {
                mask = cValue >> (5 - bitsRemaining);
                curByte = (byte)(curByte | mask);
                returnArray[arrayIndex++] = curByte;
                curByte = (byte)(cValue << (3 + bitsRemaining));
                bitsRemaining += 3;
            }
        }

        if (arrayIndex != byteCount)
            returnArray[arrayIndex] = curByte;

        return returnArray;
    }

    private static int CharToValue(char c)
    {
        int value = c;

        return value switch
        {
            //uppercase letters
            < 87 and > 64 => value - 55,
            //numbers 0-9
            < 58 and > 47 => value - 48,
            //lowercase letters
            < 119 and > 96 => value - 87,
            _ => throw new ArgumentException("Character is not a Base32 character.", nameof(c))
        };
    }

    private static char ValueToChar(byte b) =>
        b switch
        {
            < 10 => (char)(b + 48),
            < 32 => (char)(b + 55),
            _ => throw new ArgumentException("Byte is not a value Base32 value.", nameof(b))
        };
}