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
    
    // /// <summary>
    // ///   Gets the hash algorithm for the <see cref="DigestType"/>.
    // /// </summary>
    // /// <param name="digestType">
    // ///   One of the <see cref="DigestType"/> values.
    // /// </param>
    // /// <returns>
    // ///   A new instance of the <see cref="HashAlgorithm"/> that implements
    // ///   the <paramref name="digestType"/>.
    // /// </returns>
    // /// <exception cref="NotImplementedException">
    // ///   When <paramref name="digestType"/> is not implemented.
    // /// </exception>
    // public static HashAlgorithm HashData(DigestType digestType)
    // {
    //     switch (digestType)
    //     {
    //         
    //     }
    //     if (Digests.TryGetValue(digestType, out DigestType maker)) 
    //     {
    //         return maker();
    //     }
    //     throw new NotImplementedException($"Digest type '{digestType}' is not implemented.");
    // }

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
    public static byte[] HashData(SecurityAlgorithm algorithm, Stream stream)
    {
        var metadata = SecurityAlgorithmRegistry.GetMetadata(algorithm);
        switch (metadata.HashAlgorithm)
        {
            case DigestType.Sha1:
                return SHA1.HashData(stream);
            case DigestType.Sha256:
                return SHA256.HashData(stream);
            case DigestType.Sha384:
                return SHA384.HashData(stream);
            case DigestType.Sha512:
                return SHA512.HashData(stream);
            case DigestType.GostR34_11_94:
            default:
                throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "The algorithm is not implemented.");
        }
    }
}