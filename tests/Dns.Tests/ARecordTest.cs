using System.Net;

using Makaretu.Dns;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsTests;

[TestClass]
public class ARecordTest
{
    [TestMethod]
    public void Roundtrip()
    {
        var a = new ARecord
        {
            Name = "emanon.org",
            Address = IPAddress.Parse("127.0.0.1")
        };
        
        var b = (ARecord)new ResourceRecord().Read(a.ToByteArray());
        
        Assert.AreEqual(a.Name, b.Name);
        Assert.AreEqual(a.Class, b.Class);
        Assert.AreEqual(a.Type, b.Type);
        Assert.AreEqual(a.TTL, b.TTL);
        Assert.AreEqual(a.Address, b.Address);
    }

    [TestMethod]
    public void Roundtrip_Master()
    {
        var a = new ARecord
        {
            Name = "emanon.org",
            Address = IPAddress.Parse("127.0.0.1")
        };
        
        var b = (ARecord)new ResourceRecord().Read(a.ToString());
        
        Assert.IsNotNull(b);
        Assert.AreEqual(a.Name, b.Name);
        Assert.AreEqual(a.Class, b.Class);
        Assert.AreEqual(a.Type, b.Type);
        Assert.AreEqual(a.TTL, b.TTL);
        Assert.AreEqual(a.Address, b.Address);
    }

    [TestMethod]
    public void Equality()
    {
        var a = new ARecord
        {
            Name = "emanon.org",
            Address = IPAddress.Parse("127.0.0.1")
        };
        
        var b = new ARecord
        {
            Name = "emanon.org",
            Address = IPAddress.Parse("127.0.0.2")
        };
        
        // ReSharper disable once EqualExpressionComparison
        Assert.IsTrue(a.Equals(a));
        Assert.IsFalse(a.Equals(b));
        Assert.IsFalse(a.Equals(null));
    }
}