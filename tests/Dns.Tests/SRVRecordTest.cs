using Makaretu.Dns;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsTests;

[TestClass]
public class SRVRecordTest
{
    [TestMethod]
    public void Roundtrip()
    {
        var a = new SRVRecord
        {
            Name = "_foobar._tcp",
            Priority = 1,
            Weight = 2,
            Port = 9,
            Target = "foobar.example.com"
        };
        
        var b = (SRVRecord)new ResourceRecord().Read(a.ToByteArray());
        
        Assert.AreEqual(a.Name, b.Name);
        Assert.AreEqual(a.Class, b.Class);
        Assert.AreEqual(a.Type, b.Type);
        Assert.AreEqual(a.TTL, b.TTL);
        Assert.AreEqual(a.Priority, b.Priority);
        Assert.AreEqual(a.Weight, b.Weight);
        Assert.AreEqual(a.Port, b.Port);
        Assert.AreEqual(a.Target, b.Target);
    }

    [TestMethod]
    public void Roundtrip_Master()
    {
        var a = new SRVRecord
        {
            Name = "_foobar._tcp",
            Priority = 1,
            Weight = 2,
            Port = 9,
            Target = "foobar.example.com"
        };
        
        var b = (SRVRecord)new ResourceRecord().Read(a.ToString());
        
        Assert.IsNotNull(b);
        Assert.AreEqual(a.Name, b.Name);
        Assert.AreEqual(a.Class, b.Class);
        Assert.AreEqual(a.Type, b.Type);
        Assert.AreEqual(a.TTL, b.TTL);
        Assert.AreEqual(a.Priority, b.Priority);
        Assert.AreEqual(a.Weight, b.Weight);
        Assert.AreEqual(a.Port, b.Port);
        Assert.AreEqual(a.Target, b.Target);
    }

    [TestMethod]
    public void Equality()
    {
        var a = new SRVRecord
        {
            Name = "_foobar._tcp",
            Priority = 1,
            Weight = 2,
            Port = 9,
            Target = "foobar.example.com"
        };
        
        var b = new SRVRecord
        {
            Name = "_foobar._tcp",
            Priority = 1,
            Weight = 2,
            Port = 9,
            Target = "foobar-x.example.com"
        };
        
        // ReSharper disable once EqualExpressionComparison
        Assert.IsTrue(a.Equals(a));
        Assert.IsFalse(a.Equals(b));
        Assert.IsFalse(a.Equals(null));
    }
}