using Makaretu.Dns;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsTests;

[TestClass]
public class TXTRecordTest
{
    [TestMethod]
    public void Roundtrip()
    {
        var a = new TXTRecord
        {
            Name = "the.printer.local",
            Strings =
            [
                "paper=A4",
                "colour=false"
            ]
        };
        var b = (TXTRecord)new ResourceRecord().Read(a.ToByteArray());
        Assert.AreEqual(a.Name, b.Name);
        Assert.AreEqual(a.Class, b.Class);
        Assert.AreEqual(a.Type, b.Type);
        Assert.AreEqual(a.TTL, b.TTL);
        CollectionAssert.AreEqual(a.Strings, b.Strings);
    }

    [TestMethod]
    public void Roundtrip_Master()
    {
        var a = new TXTRecord
        {
            Name = "the.printer.local",
            Strings =
            [
                "paper=A4",
                "colour=false",
                "foo1=a b",
                @"foo2=a\b",
                "foo3=a\""
            ]
        };
        var b = (TXTRecord)new ResourceRecord().Read(a.ToString());
        Assert.IsNotNull(b);
        Assert.AreEqual(a.Name, b.Name);
        Assert.AreEqual(a.Class, b.Class);
        Assert.AreEqual(a.Type, b.Type);
        Assert.AreEqual(a.TTL, b.TTL);
        CollectionAssert.AreEqual(a.Strings, b.Strings);
    }

    [TestMethod]
    public void NoStrings()
    {
        var a = new TXTRecord
        {
            Name = "the.printer.local"
        };
        var b = (TXTRecord)new ResourceRecord().Read(a.ToByteArray());
        Assert.AreEqual(a.Name, b.Name);
        Assert.AreEqual(a.Class, b.Class);
        Assert.AreEqual(a.Type, b.Type);
        Assert.AreEqual(a.TTL, b.TTL);
        CollectionAssert.AreEqual(a.Strings, b.Strings);
    }

    [TestMethod]
    public void Equality()
    {
        var a = new TXTRecord
        {
            Name = "the.printer.local",
            Strings =
            [
                "paper=A4",
                "colour=false"
            ]
        };
        var b = new TXTRecord
        {
            Name = "the.printer.local",
            Strings =
            [
                "paper=A4",
                "colour=true"
            ]
        };
        // ReSharper disable once EqualExpressionComparison
        Assert.IsTrue(a.Equals(a));
        Assert.IsFalse(a.Equals(b));
        Assert.IsFalse(a.Equals(null));
        Assert.AreNotEqual(a.GetHashCode(), new TXTRecord().GetHashCode());
    }
}