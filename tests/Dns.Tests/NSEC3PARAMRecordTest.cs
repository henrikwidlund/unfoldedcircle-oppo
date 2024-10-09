using System;

using Makaretu.Dns;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsTests;

[TestClass]
public class NSEC3PARAMRecordTest
{
    [TestMethod]
    public void Roundtrip()
    {
        var a = new NSEC3PARAMRecord
        {
            Name = "example",
            TTL = TimeSpan.FromDays(1),
            HashAlgorithm = DigestType.Sha1,
            Flags = 1,
            Iterations = 12,
            Salt = [0xaa, 0xbb, 0xcc, 0xdd]
        };
        
        var b = (NSEC3PARAMRecord)new ResourceRecord().Read(a.ToByteArray());
        
        Assert.AreEqual(a.Name, b.Name);
        Assert.AreEqual(a.Class, b.Class);
        Assert.AreEqual(a.Type, b.Type);
        Assert.AreEqual(a.TTL, b.TTL);
        Assert.AreEqual(a.HashAlgorithm, b.HashAlgorithm);
        Assert.AreEqual(a.Flags, b.Flags);
        Assert.AreEqual(a.Iterations, b.Iterations);
        CollectionAssert.AreEqual(a.Salt, b.Salt);
    }

    [TestMethod]
    public void Roundtrip_Master()
    {
        var a = new NSEC3PARAMRecord
        {
            Name = "example",
            TTL = TimeSpan.FromDays(1),
            HashAlgorithm = DigestType.Sha1,
            Flags = 1,
            Iterations = 12,
            Salt = [0xaa, 0xbb, 0xcc, 0xdd]
        };
        
        var b = (NSEC3PARAMRecord)new ResourceRecord().Read(a.ToString());
        
        Assert.IsNotNull(b);
        Assert.AreEqual(a.Name, b.Name);
        Assert.AreEqual(a.Class, b.Class);
        Assert.AreEqual(a.Type, b.Type);
        Assert.AreEqual(a.TTL, b.TTL);
        Assert.AreEqual(a.HashAlgorithm, b.HashAlgorithm);
        Assert.AreEqual(a.Flags, b.Flags);
        Assert.AreEqual(a.Iterations, b.Iterations);
        CollectionAssert.AreEqual(a.Salt, b.Salt);
    }

    [TestMethod]
    public void Roundtrip_Master_NullSalt()
    {
        var a = new NSEC3PARAMRecord
        {
            Name = "example",
            TTL = TimeSpan.FromDays(1),
            HashAlgorithm = DigestType.Sha1,
            Flags = 1,
            Iterations = 12
        };
        
        var b = (NSEC3PARAMRecord)new ResourceRecord().Read(a.ToString());
        
        Assert.IsNotNull(b);
        Assert.AreEqual(a.Name, b.Name);
        Assert.AreEqual(a.Class, b.Class);
        Assert.AreEqual(a.Type, b.Type);
        Assert.AreEqual(a.TTL, b.TTL);
        Assert.AreEqual(a.HashAlgorithm, b.HashAlgorithm);
        Assert.AreEqual(a.Flags, b.Flags);
        Assert.AreEqual(a.Iterations, b.Iterations);
        Assert.AreEqual(null, b.Salt);
    }

    [TestMethod]
    public void Roundtrip_Master_EmptySalt()
    {
        var a = new NSEC3PARAMRecord
        {
            Name = "example",
            TTL = TimeSpan.FromDays(1),
            HashAlgorithm = DigestType.Sha1,
            Flags = 1,
            Iterations = 12,
            Salt = []
        };
        
        var b = (NSEC3PARAMRecord)new ResourceRecord().Read(a.ToString());
        
        Assert.IsNotNull(b);
        Assert.AreEqual(a.Name, b.Name);
        Assert.AreEqual(a.Class, b.Class);
        Assert.AreEqual(a.Type, b.Type);
        Assert.AreEqual(a.TTL, b.TTL);
        Assert.AreEqual(a.HashAlgorithm, b.HashAlgorithm);
        Assert.AreEqual(a.Flags, b.Flags);
        Assert.AreEqual(a.Iterations, b.Iterations);
        Assert.AreEqual(null, b.Salt);
    }
}