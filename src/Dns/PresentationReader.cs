using System.Globalization;
using System.Net;
using System.Text;

namespace Makaretu.Dns;

/// <summary>
///   Methods to read DNS data items encoded in the presentation (text) format.
/// </summary>
public class PresentationReader
{
    private readonly TextReader _text;
    private TimeSpan? _defaultTtl;
    private DomainName? _defaultDomainName;
    private int _parenLevel;
    private int _previousChar = '\n';  // Assume a newline
    private bool _eolSeen;

    /// <summary>
    ///   Indicates that the token is at the begining of the line without
    ///   any leading whitespace.
    /// </summary>
    private bool _tokenStartsNewLine;

    /// <summary>
    ///   Creates a new instance of the <see cref="PresentationReader"/> using the
    ///   specified <see cref="System.IO.TextReader"/>.
    /// </summary>
    /// <param name="text">
    ///   The source for data items.
    /// </param>
    public PresentationReader(TextReader text) => _text = text;

    /// <summary>
    ///   The origin domain name, sometimes called the zone name.
    /// </summary>
    /// <value>
    ///   Defaults to "".
    /// </value>
    /// <remarks>
    ///   <b>Origin</b> is used when the domain name "@" is used
    ///   for a domain name.
    /// </remarks>
    public DomainName Origin { get; set; } = DomainName.Root;

    /// <summary>
    ///   Read a byte.
    /// </summary>
    /// <returns>
    ///   The number as a byte.
    /// </returns>
    public byte ReadByte() => byte.Parse(ReadToken(), CultureInfo.InvariantCulture);

    /// <summary>
    ///   Read an unsigned short.
    /// </summary>
    /// <returns>
    ///   The number as an unsigned short.
    /// </returns>
    public ushort ReadUInt16() => ushort.Parse(ReadToken(), CultureInfo.InvariantCulture);

    /// <summary>
    ///   Read an unsigned int.
    /// </summary>
    /// <returns>
    ///   The number as an unsignd int.
    /// </returns>
    public uint ReadUInt32() => uint.Parse(ReadToken(), CultureInfo.InvariantCulture);

    /// <summary>
    ///   Read a domain name.
    /// </summary>
    /// <returns>
    ///   The domain name as a string.
    /// </returns>
    public DomainName ReadDomainName() => MakeAbsoluteDomainName(ReadToken(ignoreEscape: true));

    private DomainName MakeAbsoluteDomainName(string name) =>
        name.EndsWith('.')
            // If an absolute name.
            ? new DomainName(name[..^1]) :
            // Then it's a relative name.
            DomainName.Join(new DomainName(name), Origin);

    /// <summary>
    ///   Read a string.
    /// </summary>
    /// <returns>
    ///   The string.
    /// </returns>
    public string ReadString() => ReadToken();

    /// <summary>
    ///   Read bytes encoded in base-64.
    /// </summary>
    /// <returns>
    ///   The bytes.
    /// </returns>
    /// <remarks>
    ///   This must be the last field in the RDATA because the string
    ///   can contain embedded spaces.
    /// </remarks>
    public byte[] ReadBase64String()
    {
        // Handle embedded space and CRLFs inside of parens.
        var sb = new StringBuilder();
        while (!IsEndOfLine())
            sb.Append(ReadToken());
        
        return Convert.FromBase64String(sb.ToString());
    }

    /// <summary>
    ///   Read a time span (interval) in 16-bit seconds.
    /// </summary>
    /// <returns>
    ///   A <see cref="TimeSpan"/> with second resolution.
    /// </returns>
    public TimeSpan ReadTimeSpan16() => TimeSpan.FromSeconds(ReadUInt16());

    /// <summary>
    ///   Read a time span (interval) in 32-bit seconds.
    /// </summary>
    /// <returns>
    ///   A <see cref="TimeSpan"/> with second resolution.
    /// </returns>
    public TimeSpan ReadTimeSpan32() => TimeSpan.FromSeconds(ReadUInt32());

    /// <summary>
    ///   Read an Internet address.
    /// </summary>
    /// <param name="length">
    ///   Ignored.
    /// </param>
    /// <returns>
    ///   An <see cref="IPAddress"/>.
    /// </returns>
    public IPAddress ReadIPAddress(int length = 4) => IPAddress.Parse(ReadToken());

    /// <summary>
    ///   Read a DNS Type.
    /// </summary>
    /// <remarks>
    ///   Either the name of a <see cref="DnsType"/> or
    ///   the string "TYPEx".
    /// </remarks>
    public DnsType ReadDnsType()
    {
        var token = ReadToken();
        return token.StartsWith("TYPE", StringComparison.Ordinal)
            ? (DnsType)ushort.Parse(token.AsSpan(4), CultureInfo.InvariantCulture)
            : DnsTypeExtensions.Parse(token);
    }

    /// <summary>
    ///   Read a date/time.
    /// </summary>
    /// <returns>
    ///   The <see cref="DateTime"/>.
    /// </returns>
    /// <remarks>
    ///   Allows a <see cref="DateTime"/> in the form "yyyyMMddHHmmss" or
    ///   the number of seconds since the unix epoch (00:00:00 on 1 January 1970 UTC).
    /// </remarks>
    public DateTime ReadDateTime()
    {
        var token = ReadToken();
        return token.Length == 14
            ? DateTime.ParseExact(
                token,
                "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal)
            : DateTimeOffset.UnixEpoch.UtcDateTime.AddSeconds(ulong.Parse(token, CultureInfo.InvariantCulture));
    }

    /// <summary>
    ///   Read hex encoded RDATA.
    /// </summary>
    /// <returns>
    ///   A byte array containing the RDATA.
    /// </returns>
    /// <remarks>
    ///   See <see href="https://tools.ietf.org/html/rfc3597#section-5"/> for all
    ///   the details.
    /// </remarks>
    public byte[] ReadResourceData()
    {
        var leadin = ReadToken();
        if (!leadin.Equals("#", StringComparison.Ordinal))
            throw new FormatException($"Expected RDATA leadin '\\#', not '{leadin}'.");
        
        var length = ReadUInt32();
        if (length == 0)
            return [];

        // Get the hex string.
        var sb = new StringBuilder();
        while (sb.Length < length * 2)
        {
            var word = ReadToken();
            if (word.Length == 0)
                break;
            
            if (word.Length % 2 != 0)
                throw new FormatException($"The hex word ('{word}') must have an even number of digits.");
            
            sb.Append(word);
        }
        
        if (sb.Length != length * 2)
            throw new FormatException("Wrong number of RDATA hex digits.");

        // Convert hex string into byte array.
        try
        {
            return BaseConvert.FromBase16(sb.ToString());
        }
        catch (InvalidOperationException e)
        {
            throw new FormatException(e.Message);
        }
    }

    /// <summary>
    ///   Read a resource record.
    /// </summary>
    /// <returns>
    ///   A <see cref="ResourceRecord"/> or <b>null</b> if no more
    ///   resource records are available.
    /// </returns>
    /// <remarks>
    ///   Processes the "$ORIGIN" and "$TTL" specials that define the
    ///   <see cref="Origin"/> and a default time-to-live respectively.
    ///   <para>
    ///   A domain name can be "@" to refer to the <see cref="Origin"/>. 
    ///   A missing domain name will use the previous record's domain name.
    ///   </para>
    ///   <para>
    ///   Defaults the <see cref="ResourceRecord.Class"/> to <see cref="DnsClass.IN"/>.
    ///   Defaults the <see cref="ResourceRecord.TTL"/>  to either the "$TTL" or
    ///   the <see cref="ResourceRecord.DefaultTTL"/>.
    ///   </para>
    /// </remarks>
    public ResourceRecord? ReadResourceRecord()
    {
        var domainName = _defaultDomainName;
        var dnsClass = DnsClass.IN;
        var ttl = _defaultTtl;
        DnsType? type = null;

        while (!type.HasValue)
        {
            var token = ReadToken(ignoreEscape: true);
            if (string.Equals(token, string.Empty, StringComparison.Ordinal))
                return null;

            // Domain names and directives must be at the start of a line.
            if (_tokenStartsNewLine)
            {
                switch (token)
                {
                    case "$ORIGIN":
                        Origin = ReadDomainName();
                        break;
                    case "$TTL":
                        _defaultTtl = ttl = ReadTimeSpan32();
                        break;
                    case "@":
                        domainName = Origin;
                        _defaultDomainName = domainName;
                        break;
                    default:
                        domainName = MakeAbsoluteDomainName(token);
                        _defaultDomainName = domainName;
                        break;
                }
                continue;
            }

            // Is TTL?
            if (token.All(static c => char.IsDigit(c)))
            {
                ttl = TimeSpan.FromSeconds(uint.Parse(token));
                continue;
            }

            // Is TYPE?
            if (token.StartsWith("TYPE", StringComparison.Ordinal))
            {
                type = (DnsType)ushort.Parse(token.AsSpan(4), CultureInfo.InvariantCulture);
                continue;
            }
            
            if (!token.Equals("any", StringComparison.InvariantCultureIgnoreCase) && Enum.TryParse<DnsType>(token, out var t))
            {
                type = t;
                continue;
            }

            // Is CLASS?
            if (token.StartsWith("CLASS", StringComparison.Ordinal))
            {
                dnsClass = (DnsClass)ushort.Parse(token.AsSpan(5), CultureInfo.InvariantCulture);
                continue;
            }

            if (!DnsClassExtensions.TryParse(token, out var c))
                throw new InvalidDataException($"Unknown token '{token}', expected a Class, Type or TTL.");

            dnsClass = c;

        }

        if (domainName is null)
            throw new InvalidDataException("Missing resource record name.");

        // Create the specific resource record based on the type.
        var resource = ResourceRegistry.Create(type.Value);
        resource.Name = domainName;
        resource.Type = type.Value;
        resource.Class = dnsClass;

        if (ttl.HasValue)
            resource.TTL = ttl.Value;

        // Read the specific properties of the resource record.
        resource.ReadData(this);

        return resource;
    }

    /// <summary>
    ///   Determines if the reader is at the end of a line.
    /// </summary>
    public bool IsEndOfLine()
    {
        int c;
        while (_parenLevel > 0)
        {
            while ((c = _text.Peek()) >= 0)
            {
                if (c is ' ' or '\t' or '\r' or '\n')
                {
                    c = _text.Read();
                    _previousChar = c;
                    continue;
                }

                if (c != ')')
                    return false;

                --_parenLevel;
                c = _text.Read();
                _previousChar = c;
                break;

            }
        }

        if (_eolSeen)
            return true;

        while ((c = _text.Peek()) >= 0)
        {
            // Skip space or tab.
            if (c is not (' ' or '\t'))
                return c is '\r' or '\n' or ';';

            c = _text.Read();
            _previousChar = c;
        }

        // EOF is end of line
        return true;
    }

    private string ReadToken(bool ignoreEscape = false)
    {
        var sb = new StringBuilder();
        int c;
        var skipWhitespace = true;
        var inquote = false;
        var incomment = false;
        _eolSeen = false;

        while ((c = _text.Read()) >= 0)
        {
            // Comments are terminated by a newline.
            if (incomment)
            {
                if (c is '\r' or '\n')
                {
                    incomment = false;
                    skipWhitespace = true;
                }
                
                _previousChar = c;
                continue;
            }

            // Handle escaped character.
            if (c == '\\')
            {
                if (ignoreEscape)
                {
                    if (sb.Length == 0)
                        _tokenStartsNewLine = _previousChar is '\r' or '\n';
                    
                    sb.Append((char)c);
                    _previousChar = c;

                    c = _text.Read();
                    if (0 <= c)
                    {
                        sb.Append((char)c);
                        _previousChar = c;
                    }
                    
                    continue;
                }
                _previousChar = c;

                // Handle decimal escapes \DDD
                var ndigits = 0;
                var ddd = 0;
                for (; ndigits <= 3; ++ndigits)
                {
                    c = _text.Peek();
                    if (c is >= '0' and <= '9')
                    {
                        _text.Read();
                        ddd = ddd * 10 + (c - '0');
                        
                        if (ddd > 0xFF)
                            throw new FormatException("Invalid value.");
                    }
                    else
                        break;
                }
                
                c = ndigits > 0 ? ddd : _text.Read();

                sb.Append((char)c);
                skipWhitespace = false;
                _previousChar = (char)c;
                
                continue;
            }

            // Handle quoted strings.
            if (inquote)
            {
                if (c == '"')
                    break;

                sb.Append((char)c);
                _previousChar = c;
                continue;
            }
            
            if (c == '"')
            {
                inquote = true;
                _previousChar = c;
                continue;
            }

            // Ignore parens.
            if (c == '(')
            {
                ++_parenLevel;
                c = ' ';
            }
            
            if (c == ')')
            {
                --_parenLevel;
                c = ' ';
            }

            // Skip leading whitespace.
            if (skipWhitespace)
            {
                if (char.IsWhiteSpace((char)c))
                {
                    _previousChar = c;
                    continue;
                }
                
                skipWhitespace = false;
            }

            // Trailing whitespace, ends the token.
            if (char.IsWhiteSpace((char)c))
            {
                _previousChar = c;
                _eolSeen = c is '\r' or '\n';
                break;
            }

            // Handle start of comment.
            if (c == ';')
            {
                incomment = true;
                _previousChar = c;
                continue;
            }

            // Default handling, use the character as part of the token.
            if (sb.Length == 0)
                _tokenStartsNewLine = _previousChar is '\r' or '\n';
            
            sb.Append((char)c);
            _previousChar = c;
        }

        return sb.ToString();
    }
}