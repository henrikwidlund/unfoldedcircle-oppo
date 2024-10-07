using Makaretu.Dns;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsTests;

[TestClass]
public class MXRecordTest
{
    [TestMethod]
    public void Roundtrip()
    {
        var a = new MXRecord
        {
            Name = "emanon.org",
            Preference = 10,
            Exchange = "mail.emanon.org"
        };
        
        var b = (MXRecord)new ResourceRecord().Read(a.ToByteArray());
        
        Assert.AreEqual(a.Name, b.Name);
        Assert.AreEqual(a.Class, b.Class);
        Assert.AreEqual(a.Type, b.Type);
        Assert.AreEqual(a.TTL, b.TTL);
        Assert.AreEqual(a.Preference, b.Preference);
        Assert.AreEqual(a.Exchange, b.Exchange);
    }

    [TestMethod]
    public void Roundtrip_Master()
    {
        var a = new MXRecord
        {
            Name = "emanon.org",
            Preference = 10,
            Exchange = "mail.emanon.org"
        };
        
        var b = (MXRecord)new ResourceRecord().Read(a.ToString());
        
        Assert.IsNotNull(b);
        Assert.AreEqual(a.Name, b.Name);
        Assert.AreEqual(a.Class, b.Class);
        Assert.AreEqual(a.Type, b.Type);
        Assert.AreEqual(a.TTL, b.TTL);
        Assert.AreEqual(a.Preference, b.Preference);
        Assert.AreEqual(a.Exchange, b.Exchange);
    }

    [TestMethod]
    public void Equality()
    {
        var a = new MXRecord
        {
            Name = "emanon.org",
            Preference = 10,
            Exchange = "mail.emanon.org"
        };
        
        var b = new MXRecord
        {
            Name = "emanon.org",
            Preference = 11,
            Exchange = "mailx.emanon.org"
        };
        
        // ReSharper disable once EqualExpressionComparison
        Assert.IsTrue(a.Equals(a));
        Assert.IsFalse(a.Equals(b));
        Assert.IsFalse(a.Equals(null));
    }
}