﻿using System.Globalization;
using System.Net;

namespace Makaretu.Dns;

/// <summary>
///   Methods to write DNS data items encoded in the presentation (text) format.
/// </summary>
public class PresentationWriter
{
    private readonly TextWriter _text;

    /// <summary>
    ///   Creates a new instance of the <see cref="PresentationWriter"/> using the
    ///   specified <see cref="TextWriter"/>.
    /// </summary>
    /// <param name="text">
    ///   The source for data items.
    /// </param>
    public PresentationWriter(TextWriter text) => _text = text;

    /// <summary>
    ///   Writes a space.
    /// </summary>
    public void WriteSpace() => _text.Write(' ');

    /// <summary>
    ///   Writes a CRLF.
    /// </summary>
    public void WriteEndOfLine() => _text.Write("\r\n");

    /// <summary>
    ///   Write an byte.
    /// </summary>
    /// <param name="value">
    ///   The value to write.
    /// </param>
    /// <param name="appendSpace">
    ///   Write a space after the value.
    /// </param>
    public void WriteByte(byte value, bool appendSpace = true)
    {
        _text.Write(value);
        
        if (appendSpace)
            WriteSpace();
    }

    /// <summary>
    ///   Write an unsigned short.
    /// </summary>
    /// <param name="value">
    ///   The value to write.
    /// </param>
    /// <param name="appendSpace">
    ///   Write a space after the value.
    /// </param>
    public void WriteUInt16(ushort? value, bool appendSpace = true)
    {
        if (value is null)
            return;
        
        _text.Write(value.ToString());
        
        if (appendSpace)
            WriteSpace();
    }

    /// <summary>
    ///   Write an unsigned int.
    /// </summary>
    /// <param name="value">
    ///   The value to write.
    /// </param>
    /// <param name="appendSpace">
    ///   Write a space after the value.
    /// </param>
    public void WriteUInt32(uint value, bool appendSpace = true)
    {
        _text.Write(value);
        
        if (appendSpace)
            WriteSpace();
    }

    /// <summary>
    ///   Write a string.
    /// </summary>
    /// <param name="value">
    ///   An ASCII string.
    /// </param>
    /// <param name="appendSpace">
    ///   Write a space after the value.
    /// </param>
    /// <remarks>
    ///   Quotes and escapes are added as needed.
    /// </remarks>
    public void WriteString(string? value, bool appendSpace = true)
    {
        bool needQuote = false;

        value ??= string.Empty;
        
        if (value == string.Empty)
            needQuote = true;
        
        value = value.Replace("\\", @"\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
        if (value.Contains(' ', StringComparison.Ordinal))
            needQuote = true;
        
        if (needQuote)
            _text.Write('"');
        
        _text.Write(value);
        
        if (needQuote)
            _text.Write('"');
        
        if (appendSpace)
            WriteSpace();
    }

    /// <summary>
    ///   Write a string.
    /// </summary>
    /// <param name="value">
    ///   An ASCII string.
    /// </param>
    /// <param name="appendSpace">
    ///   Write a space after the value.
    /// </param>
    /// <remarks>
    ///   Quotes and escapes are NOT added.
    /// </remarks>
    public void WriteStringUnencoded(string value, bool appendSpace = true)
    {
        _text.Write(value);
        
        if (appendSpace)
            WriteSpace();
    }

    /// <summary>
    ///   Write a domain name.
    /// </summary>
    /// <param name="value">
    ///   The value to write.
    /// </param>
    /// <param name="appendSpace">
    ///   Write a space after the value.
    /// </param>
    public void WriteDomainName(DomainName? value, bool appendSpace = true)
    {
        if (value is not null)
            WriteStringUnencoded(value.ToString(), appendSpace);
    }

    /// <summary>
    ///   Write bytes encoded in base-16.
    /// </summary>
    /// <param name="value">
    ///   The value to write.
    /// </param>
    /// <param name="appendSpace">
    ///   Write a space after the value.
    /// </param>
    public void WriteBase16String(byte[] value, bool appendSpace = true) => WriteString(BaseConvert.ToBase16Lower(value), appendSpace);

    /// <summary>
    ///   Write bytes encoded in base-64.
    /// </summary>
    /// <param name="value">
    ///   The value to write.
    /// </param>
    /// <param name="appendSpace">
    ///   Write a space after the value.
    /// </param>
    public void WriteBase64String(byte[]? value, bool appendSpace = true)
    {
        if (value is null)
            return;
        
        WriteString(Convert.ToBase64String(value), appendSpace);
    }

    /// <summary>
    ///   Write a time span (interval) in 16-bit seconds.
    /// </summary>
    /// <param name="value">
    ///   The number of seconds to write.
    /// </param>
    /// <param name="appendSpace">
    ///   Write a space after the value.
    /// </param>
    public void WriteTimeSpan16(TimeSpan value, bool appendSpace = true) => WriteUInt16((ushort)value.TotalSeconds, appendSpace);

    /// <summary>
    ///   Write a time span (interval) in 32-bit seconds.
    /// </summary>
    /// <param name="value">
    ///   The number of seconds to write.
    /// </param>
    /// <param name="appendSpace">
    ///   Write a space after the value.
    /// </param>
    public void WriteTimeSpan32(TimeSpan value, bool appendSpace = true) => WriteUInt32((uint)value.TotalSeconds, appendSpace);

    /// <summary>
    ///   Write a date/time.
    /// </summary>
    /// <param name="value">
    ///   The UTC <see cref="DateTime"/>. Resolution is seconds.
    /// </param>
    /// <param name="appendSpace">
    ///   Write a space after the value.
    /// </param>
    public void WriteDateTime(DateTime value, bool appendSpace = true) =>
        WriteString(value.ToUniversalTime().ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture), appendSpace);

    /// <summary>
    ///   Write an Internet address.
    /// </summary>
    /// <param name="value">
    ///   The value to write.
    /// </param>
    /// <param name="appendSpace">
    ///   Write a space after the value.
    /// </param>
    public void WriteIPAddress(IPAddress value, bool appendSpace = true) => WriteString(value.ToString(), appendSpace);

    /// <summary>
    ///   Write a DNS Type.
    /// </summary>
    /// <param name="value">
    ///   The value to write.
    /// </param>
    /// <param name="appendSpace">
    ///   Write a space after the value.
    /// </param>
    /// <remarks>
    ///   Either the name of a <see cref="DnsType"/> or
    ///   the string "TYPEx".
    /// </remarks>
    public void WriteDnsType(DnsType value, bool appendSpace = true)
    {
        if (!DnsTypeExtensions.IsDefined(value))
            _text.Write("TYPE");
        
        _text.Write(value.ToStringFast());
        
        if (appendSpace)
            WriteSpace();
    }

    /// <summary>
    ///   Write a DNS Class.
    /// </summary>
    /// <param name="value">
    ///   The value to write.
    /// </param>
    /// <param name="appendSpace">
    ///   Write a space after the value.
    /// </param>
    /// <remarks>
    ///   Either the name of a <see cref="DnsClass"/> or
    ///   the string "CLASSx".
    /// </remarks>
    public void WriteDnsClass(DnsClass value, bool appendSpace = true)
    {
        if (!DnsClassExtensions.IsDefined(value))
            _text.Write("CLASS");
        
        _text.Write(value.ToStringFast());
        
        if (appendSpace)
            WriteSpace();
    }
}