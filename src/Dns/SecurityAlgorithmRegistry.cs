namespace Makaretu.Dns;

/// <summary>
///   Registry of implemented <see cref="SecurityAlgorithm"/>.
/// </summary>
/// <remarks>
///   IANA maintains a list of all known types at <see href="https://www.iana.org/assignments/dns-sec-alg-numbers/dns-sec-alg-numbers.xhtml#dns-sec-alg-numbers-1"/>.
/// </remarks>
/// <see cref="SecurityAlgorithm"/>
public static class SecurityAlgorithmRegistry
{
    /// <summary>
    ///   Metadata on a <see cref="SecurityAlgorithm"/>.
    /// </summary>
    /// <remarks>
    ///   Used by the <see cref="SecurityAlgorithmRegistry"/>.
    /// </remarks>
    public class Metadata
    {
        /// <summary>
        ///   The cryptographic hash algorithm to use.
        /// </summary>
        public DigestType HashAlgorithm { get; set; }

        /// <summary>
        ///   Other names associated with the algorithm.
        /// </summary>
        public string[] OtherNames { get; set; } = [];
    }

    /// <summary>
    ///   Defined security algorithms.
    /// </summary>
    /// <remarks>
    ///   The key is the <see cref="SecurityAlgorithm"/>.
    ///   The value is th <see cref="Metadata"/>.
    /// </remarks>
    public static readonly Dictionary<SecurityAlgorithm, Metadata> Algorithms;
    
    static SecurityAlgorithmRegistry()
    {
        Algorithms = new Dictionary<SecurityAlgorithm, Metadata>
        {
            {
                SecurityAlgorithm.RSASHA1, new Metadata
                {
                    HashAlgorithm = DigestType.Sha1
                }
            },
            {
                SecurityAlgorithm.RSASHA256, new Metadata
                {
                    HashAlgorithm = DigestType.Sha256
                }
            },
            { SecurityAlgorithm.RSASHA512, new Metadata
            {
                HashAlgorithm = DigestType.Sha512
            } },
            { SecurityAlgorithm.DSA, new Metadata
            {
                HashAlgorithm = DigestType.Sha1
            } },
            { SecurityAlgorithm.ECDSAP256SHA256, new Metadata
            {
                HashAlgorithm = DigestType.Sha256,
                OtherNames = ["nistP256", "ECDSA_P256"]
            } },
            { SecurityAlgorithm.ECDSAP384SHA384, new Metadata
            {
                HashAlgorithm = DigestType.Sha384,
                OtherNames = ["nistP384", "ECDSA_P384"]
            } }
        };

        Algorithms.Add(SecurityAlgorithm.RSASHA1NSEC3SHA1, Algorithms[SecurityAlgorithm.RSASHA1]);
        Algorithms.Add(SecurityAlgorithm.DSANSEC3SHA1, Algorithms[SecurityAlgorithm.DSA]);
    }


    /// <summary>
    ///   Gets the meta data for the the <see cref="SecurityAlgorithm"/>.
    /// </summary>
    /// <param name="algorithm">
    ///   One of the <see cref="SecurityAlgorithm"/> values.
    /// </param>
    /// <returns>
    ///   The <see cref="Metadata"/> for the <paramref name="algorithm"/>.
    /// </returns>
    /// <exception cref="NotImplementedException">
    ///   When the <paramref name="algorithm"/> is not defined.
    /// </exception>
    public static Metadata GetMetadata(SecurityAlgorithm algorithm)
    {
        if (Algorithms.TryGetValue(algorithm, out var metadata))
        {
            return metadata;
        }
        throw new NotImplementedException($"The security algorithm '{algorithm}' is not defined.");
    }
}