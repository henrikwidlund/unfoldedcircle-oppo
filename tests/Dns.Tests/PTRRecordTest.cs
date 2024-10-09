using Makaretu.Dns;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsTests;

[TestClass]
public class PTRRecordTest
{
    [TestMethod]
    public void Roundtrip()
    {
        var a = new PTRRecord
        {
            Name = "emanon.org",
            DomainName = "somewhere.else.org"
        };
        
        var b = (PTRRecord)new ResourceRecord().Read(a.ToByteArray());
        
        Assert.AreEqual(a.Name, b.Name);
        Assert.AreEqual(a.Class, b.Class);
        Assert.AreEqual(a.Type, b.Type);
        Assert.AreEqual(a.TTL, b.TTL);
        Assert.AreEqual(a.DomainName, b.DomainName);
    }

    [TestMethod]
    public void Roundtrip_Master()
    {
        var a = new PTRRecord
        {
            Name = "emanon.org",
            DomainName = "somewhere.else.org"
        };
        
        var b = (PTRRecord)new ResourceRecord().Read(a.ToString());
        
        Assert.IsNotNull(b);
        Assert.AreEqual(a.Name, b.Name);
        Assert.AreEqual(a.Class, b.Class);
        Assert.AreEqual(a.Type, b.Type);
        Assert.AreEqual(a.TTL, b.TTL);
        Assert.AreEqual(a.DomainName, b.DomainName);
    }

    [TestMethod]
    public void Equality()
    {
        var a = new PTRRecord
        {
            Name = "emanon.org",
            DomainName = "somewhere.else.org"
        };
        
        var b = new PTRRecord
        {
            Name = "emanon.org",
            DomainName = "somewhere.org"
        };
        
        // ReSharper disable once EqualExpressionComparison
        Assert.IsTrue(a.Equals(a));
        Assert.IsFalse(a.Equals(b));
        Assert.IsFalse(a.Equals(null));
    }
}