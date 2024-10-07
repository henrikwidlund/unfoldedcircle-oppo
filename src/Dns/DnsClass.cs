using System.Diagnostics.CodeAnalysis;

namespace Makaretu.Dns;

/// <summary>
///  Identifies the network of the <see cref="ResourceRecord"/>.
/// </summary>
/// <remarks>
///   The values are maintained by IANA at <see href="https://www.iana.org/assignments/dns-parameters/dns-parameters.xhtml#dns-parameters-2"/>.
/// </remarks>
public enum DnsClass : ushort
{
    /// <summary>
    ///   The Internet.
    /// </summary>
    IN = 1,

    /// <summary>
    ///   The CSNET class (Obsolete - used only for examples insome obsolete RFCs).
    /// </summary>
    CS = 2,

    /// <summary>
    ///   The CHAOS class.
    /// </summary>
    CH = 3,

    /// <summary>
    ///   Hesiod[Dyer 87].
    /// </summary>
    HS = 4,

    /// <summary>
    ///   Used in UPDATE message to signify no class.
    /// </summary>
    None = 254,

    /// <summary>
    ///   Only used in QCLASS.
    /// </summary>
    /// <seealso cref="Question.Class"/>
    ANY = 255
}

public static class DnsClassExtensions
{
    public static bool TryParse(
        [NotNullWhen(true)]
        string? name,
        out DnsClass value)
    {
        switch (name)
        {
            case not null when name.Equals(nameof(DnsClass.IN), StringComparison.OrdinalIgnoreCase):
                value = DnsClass.IN;
                return true;
            case not null when name.Equals(nameof(DnsClass.CS), StringComparison.OrdinalIgnoreCase):
                value = DnsClass.CS;
                return true;
            case not null when name.Equals(nameof(DnsClass.CH), StringComparison.OrdinalIgnoreCase):
                value = DnsClass.CH;
                return true;
            case not null when name.Equals(nameof(DnsClass.HS), StringComparison.OrdinalIgnoreCase):
                value = DnsClass.HS;
                return true;
            case not null when name.Equals(nameof(DnsClass.None), StringComparison.OrdinalIgnoreCase):
                value = DnsClass.None;
                return true;
            case not null when name.Equals(nameof(DnsClass.ANY), StringComparison.OrdinalIgnoreCase):
                value = DnsClass.ANY;
                return true;
            case not null when ushort.TryParse(name, out var val):
                value = (DnsClass)val;
                return true;
            default:
                value = default;
                return false;
        }
    }

    public static bool IsDefined(DnsClass value)
        => value switch
        {
            DnsClass.IN => true,
            DnsClass.CS => true,
            DnsClass.CH => true,
            DnsClass.HS => true,
            DnsClass.None => true,
            DnsClass.ANY => true,
            _ => false
        };
    
    public static string ToStringFast(this DnsClass value)
        => value switch
        {
            DnsClass.IN => nameof(DnsClass.IN),
            DnsClass.CS => nameof(DnsClass.CS),
            DnsClass.CH => nameof(DnsClass.CH),
            DnsClass.HS => nameof(DnsClass.HS),
            DnsClass.None => nameof(DnsClass.None),
            DnsClass.ANY => nameof(DnsClass.ANY),
            _ => value.ToString()
        };
}