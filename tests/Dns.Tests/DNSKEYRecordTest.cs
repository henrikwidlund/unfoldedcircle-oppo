using System;
using System.Security.Cryptography;

using Makaretu.Dns;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsTests;

[TestClass]
public class DNSKEYRecordTest
{
    private static readonly byte[] Key = Convert.FromBase64String("AQPSKmynfzW4kyBv015MUG2DeIQ3Cbl+BBZH4b/0PY1kxkmvHjcZc8nokfzj31GajIQKY+5CptLr3buXA10hWqTkF7H6RfoRqXQeogmMHfpftf6zMv1LyBUgia7za6ZEzOJBOztyvhjL742iU/TpPSEDhm2SNKLijfUppn1UaNvv4w==");

    [TestMethod]
    public void Roundtrip()
    {
        var a = new DNSKEYRecord
        {
            Name = "example.com",
            TTL = TimeSpan.FromDays(2),
            Flags = DNSKEYFlags.ZoneKey,
            Protocol = 3,
            Algorithm = SecurityAlgorithm.RSASHA1,
            PublicKey = Key
        };
        
        var b = (DNSKEYRecord)new ResourceRecord().Read(a.ToByteArray());
        
        Assert.AreEqual(a.Name, b.Name);
        Assert.AreEqual(a.Class, b.Class);
        Assert.AreEqual(a.Type, b.Type);
        Assert.AreEqual(a.TTL, b.TTL);
        Assert.AreEqual(a.Flags, b.Flags);
        Assert.AreEqual(a.Protocol, b.Protocol);
        Assert.AreEqual(a.Algorithm, b.Algorithm);
        CollectionAssert.AreEqual(a.PublicKey, b.PublicKey);
    }

    [TestMethod]
    public void Roundtrip_Master()
    {
        var a = new DNSKEYRecord
        {
            Name = "example.com",
            TTL = TimeSpan.FromDays(2),
            Flags = DNSKEYFlags.ZoneKey,
            Protocol = 3,
            Algorithm = SecurityAlgorithm.RSASHA1,
            PublicKey = Key
        };
        
        var b = (DNSKEYRecord)new ResourceRecord().Read(a.ToString());
        
        Assert.IsNotNull(b);
        Assert.AreEqual(a.Name, b.Name);
        Assert.AreEqual(a.Class, b.Class);
        Assert.AreEqual(a.Type, b.Type);
        Assert.AreEqual(a.TTL, b.TTL);
        Assert.AreEqual(a.Flags, b.Flags);
        Assert.AreEqual(a.Protocol, b.Protocol);
        Assert.AreEqual(a.Algorithm, b.Algorithm);
        CollectionAssert.AreEqual(a.PublicKey, b.PublicKey);
    }

    [TestMethod]
    public void KeyTag()
    {
        // From https://tools.ietf.org/html/rfc4034#section-5.4
        var a = new DNSKEYRecord
        {
            Name = "example.com",
            TTL = TimeSpan.FromSeconds(86400),
            Flags = DNSKEYFlags.ZoneKey,
            Algorithm = SecurityAlgorithm.RSASHA1,
            PublicKey = Convert.FromBase64String(
                """
                    AQOeiiR0GOMYkDshWoSKz9Xz
                    fwJr1AYtsmx3TGkJaNXVbfi/
                    2pHm822aJ5iI9BMzNXxeYCmZ
                    DRD99WYwYqUSdjMmmAphXdvx
                    egXd/M5+X7OrzKBaMbCVdFLU
                    Uh6DhweJBjEVv5f2wwjM9Xzc
                    nOf+EPbtG9DMBmADjFDc2w/r
                    ljwvFw==
                """)
        };
        
        Assert.AreEqual(60485, a.KeyTag());
    }

    [TestMethod]
    public void FromRsaSha256()
    {
        // From https://tools.ietf.org/html/rfc5702#section-6.1
        var modulus = Convert.FromBase64String("wVwaxrHF2CK64aYKRUibLiH30KpPuPBjel7E8ZydQW1HYWHfoGmidzC2RnhwCC293hCzw+TFR2nqn8OVSY5t2Q==");
        var publicExponent = Convert.FromBase64String("AQAB");
        var dnsPublicKey = Convert.FromBase64String("AwEAAcFcGsaxxdgiuuGmCkVImy4h99CqT7jwY3pexPGcnUFtR2Fh36BponcwtkZ4cAgtvd4Qs8PkxUdp6p/DlUmObdk=");

        var parameters = new RSAParameters
        {
            Exponent = publicExponent,
            Modulus = modulus
        };
        
        using var publicKey = RSA.Create();
        publicKey.ImportParameters(parameters);

        var dnskey = new DNSKEYRecord(publicKey, SecurityAlgorithm.RSASHA256)
        {
            Flags = DNSKEYFlags.ZoneKey
        };
        
        Assert.AreEqual(DNSKEYFlags.ZoneKey, dnskey.Flags);
        Assert.AreEqual(3, dnskey.Protocol);
        Assert.AreEqual(SecurityAlgorithm.RSASHA256, dnskey.Algorithm);
        CollectionAssert.AreEqual(dnsPublicKey, dnskey.PublicKey);
        Assert.AreEqual(9033, dnskey.KeyTag());
    }

    [TestMethod]
    public void FromRsaSha256_BadAlgorithm()
    {
        // From https://tools.ietf.org/html/rfc5702#section-6.1
        var modulus = Convert.FromBase64String("wVwaxrHF2CK64aYKRUibLiH30KpPuPBjel7E8ZydQW1HYWHfoGmidzC2RnhwCC293hCzw+TFR2nqn8OVSY5t2Q==");
        var publicExponent = Convert.FromBase64String("AQAB");

        var parameters = new RSAParameters
        {
            Exponent = publicExponent,
            Modulus = modulus
        };
        
        var publicKey = RSA.Create();
        publicKey.ImportParameters(parameters);

        ExceptionAssert.Throws<ArgumentException>(() =>
        {
            _ = new DNSKEYRecord(publicKey, SecurityAlgorithm.ECDSAP256SHA256);
        });
    }

    [TestMethod]
    public void FromRsaSha512()
    {
        // From https://tools.ietf.org/html/rfc5702#section-6.2
        var modulus = Convert.FromBase64String("0eg1M5b563zoq4k5ZEOnWmd2/BvpjzedJVdfIsDcMuuhE5SQ3pfQ7qmdaeMlC6Nf8DKGoUPGPXe06cP27/WRODtxXquSUytkO0kJDk8KX8PtA0+yBWwy7UnZDyCkynO00Uuk8HPVtZeMO1pHtlAGVnc8VjXZlNKdyit99waaE4s=");
        var publicExponent = Convert.FromBase64String("AQAB");
        var dnsPublicKey = Convert.FromBase64String("AwEAAdHoNTOW+et86KuJOWRDp1pndvwb6Y83nSVXXyLA3DLroROUkN6X0O6pnWnjJQujX/AyhqFDxj13tOnD9u/1kTg7cV6rklMrZDtJCQ5PCl/D7QNPsgVsMu1J2Q8gpMpztNFLpPBz1bWXjDtaR7ZQBlZ3PFY12ZTSncorffcGmhOL");

        var parameters = new RSAParameters
        {
            Exponent = publicExponent,
            Modulus = modulus
        };
        
        var publicKey = RSA.Create();
        publicKey.ImportParameters(parameters);

        var dnskey = new DNSKEYRecord(publicKey, SecurityAlgorithm.RSASHA512)
        {
            Flags = DNSKEYFlags.ZoneKey
        };
        
        Assert.AreEqual(DNSKEYFlags.ZoneKey, dnskey.Flags);
        Assert.AreEqual(3, dnskey.Protocol);
        Assert.AreEqual(SecurityAlgorithm.RSASHA512, dnskey.Algorithm);
        CollectionAssert.AreEqual(dnsPublicKey, dnskey.PublicKey);
        Assert.AreEqual(3740, dnskey.KeyTag());
    }

    [TestMethod]
    public void FromECDsaP256()
    {
        // From https://tools.ietf.org/html/rfc6605#section-6.1
        var dnsPublicKey = Convert.FromBase64String("GojIhhXUN/u4v54ZQqGSnyhWJwaubCvTmeexv7bR6edbkrSqQpF64cYbcB7wNcP+e+MAnLr+Wi9xMWyQLc8NAA==");
        var qx = new byte[32];
        var qy = new byte[32];
        Array.Copy(dnsPublicKey, 0, qx, 0, 32);
        Array.Copy(dnsPublicKey, 32, qy, 0, 32);

        // Create the public key
        var parameters = new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint
            {
                X = qx,
                Y = qy
            }
        };
        
        ECDsa publicKey;
        try
        {
            publicKey = ECDsa.Create(parameters);
        }
        catch (NotImplementedException)
        {
            Assert.Inconclusive("Platform does not support nistP256");
            return;
        }

        var dnskey = new DNSKEYRecord(publicKey)
        {
            Flags = DNSKEYFlags.ZoneKey | DNSKEYFlags.SecureEntryPoint
        };
        
        Assert.AreEqual(DNSKEYFlags.ZoneKey | DNSKEYFlags.SecureEntryPoint, dnskey.Flags);
        Assert.AreEqual(3, dnskey.Protocol);
        Assert.AreEqual(SecurityAlgorithm.ECDSAP256SHA256, dnskey.Algorithm);
        CollectionAssert.AreEqual(dnsPublicKey, dnskey.PublicKey);
        Assert.AreEqual(55648, dnskey.KeyTag());
    }

    [TestMethod]
    public void FromECDsaP384()
    {
        // From https://tools.ietf.org/html/rfc6605#section-6.2
        var dnsPublicKey = Convert.FromBase64String("xKYaNhWdGOfJ+nPrL8/arkwf2EY3MDJ+SErKivBVSum1w/egsXvSADtNJhyem5RCOpgQ6K8X1DRSEkrbYQ+OB+v8/uX45NBwY8rp65F6Glur8I/mlVNgF6W/qTI37m40");
        var qx = new byte[48];
        var qy = new byte[48];
        Array.Copy(dnsPublicKey, 0, qx, 0, 48);
        Array.Copy(dnsPublicKey, 48, qy, 0, 48);

        // Create the public key
        var parameters = new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP384,
            Q = new ECPoint
            {
                X = qx,
                Y = qy
            }
        };
        
        ECDsa publicKey;
        try
        {
            publicKey = ECDsa.Create(parameters);
        }
        catch (NotImplementedException)
        {
            Assert.Inconclusive("Platform does not support nistP384");
            return;
        }

        var dnskey = new DNSKEYRecord(publicKey)
        {
            Flags = DNSKEYFlags.ZoneKey | DNSKEYFlags.SecureEntryPoint
        };
        
        Assert.AreEqual(DNSKEYFlags.ZoneKey | DNSKEYFlags.SecureEntryPoint, dnskey.Flags);
        Assert.AreEqual(3, dnskey.Protocol);
        Assert.AreEqual(SecurityAlgorithm.ECDSAP384SHA384, dnskey.Algorithm);
        CollectionAssert.AreEqual(dnsPublicKey, dnskey.PublicKey);
        Assert.AreEqual(10771, dnskey.KeyTag());
    }
}