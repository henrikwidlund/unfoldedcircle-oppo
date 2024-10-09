using System.Security.Cryptography;

namespace Makaretu.Dns;

/// <summary>
///   Registry of implemented <see cref="DigestType"/>.
/// </summary>
/// <remarks>
///   IANA maintains a list of all known types at <see href="https://www.iana.org/assignments/ds-rr-types/ds-rr-types.xhtml#ds-rr-types-1"/>.
/// </remarks>
/// <see cref="DigestType"/>
/// <see cref="HashAlgorithm"/>
public static class DigestRegistry
{
    /// <summary>
    ///   Defined hashing algorithms.
    /// </summary>
    /// <remarks>
    ///   The key is the <see cref="DigestType"/>.
    ///   The value is a function that returns a new <see cref="ResourceRecord"/>.
    /// </remarks>
    public static readonly HashSet<DigestType> Digests;
    
    static DigestRegistry()
    {
        Digests =
        [
            DigestType.Sha1,
            DigestType.Sha256,
            DigestType.Sha384,
            DigestType.Sha512
        ];
    }

    /// <summary>
    ///   Gets the hash algorithm for the <see cref="SecurityAlgorithm"/>.
    /// </summary>
    /// <param name="algorithm">
    ///   One of the <see cref="SecurityAlgorithm"/> values.
    /// </param>
    /// <param name="stream">The stream to hash.</param>
    /// <returns>
    ///   A new instance of the <see cref="HashAlgorithm"/> that is used
    ///   for the <paramref name="algorithm"/>.
    /// </returns>
    /// <exception cref="NotImplementedException">
    ///   When the <paramref name="algorithm"/> or its <see cref="HashAlgorithm"/>
    ///   is not implemented.
    /// </exception>
    public static byte[] HashData(SecurityAlgorithm algorithm, Stream stream) =>
        SecurityAlgorithmRegistry.GetMetadata(algorithm).HashAlgorithm switch
        {
            DigestType.Sha1 => SHA1.HashData(stream),
            DigestType.Sha256 => SHA256.HashData(stream),
            DigestType.Sha384 => SHA384.HashData(stream),
            DigestType.Sha512 => SHA512.HashData(stream),
            DigestType.GostR34_11_94 => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "The algorithm is not implemented."),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "The algorithm is not implemented.")
        };
}