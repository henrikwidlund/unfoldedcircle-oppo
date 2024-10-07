using Makaretu.Dns;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsTests;

[TestClass]
public class NSRecordTest
{
    [TestMethod]
    public void Roundtrip()
    {
        var a = new NSRecord
        {
            Name = "emanon.org",
            Authority = "mydomain.name"
        };
        
        var b = (NSRecord)new ResourceRecord().Read(a.ToByteArray());
        
        Assert.AreEqual(a.Name, b.Name);
        Assert.AreEqual(a.Class, b.Class);
        Assert.AreEqual(a.Type, b.Type);
        Assert.AreEqual(a.TTL, b.TTL);
        Assert.AreEqual(a.Authority, b.Authority);
    }

    [TestMethod]
    public void Roundtrip_Master()
    {
        var a = new NSRecord
        {
            Name = "emanon.org",
            Authority = "mydomain.name"
        };
        
        var b = (NSRecord)new ResourceRecord().Read(a.ToString());
        
        Assert.IsNotNull(b);
        Assert.AreEqual(a.Name, b.Name);
        Assert.AreEqual(a.Class, b.Class);
        Assert.AreEqual(a.Type, b.Type);
        Assert.AreEqual(a.TTL, b.TTL);
        Assert.AreEqual(a.Authority, b.Authority);
    }

    [TestMethod]
    public void Equality()
    {
        var a = new NSRecord
        {
            Name = "emanon.org",
            Authority = "mydomain.name"
        };
        
        var b = new NSRecord
        {
            Name = "emanon.org",
            Authority = "mydomainx.name"
        };
        
        // ReSharper disable once EqualExpressionComparison
        Assert.IsTrue(a.Equals(a));
        Assert.IsFalse(a.Equals(b));
        Assert.IsFalse(a.Equals(null));
    }
}