using Makaretu.Dns;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsTests;

[TestClass]
public class EdnsPaddingOptionTest
{
    [TestMethod]
    public void Roundtrip()
    {
        var opt1 = new OPTRecord();
        var expected = new EdnsPaddingOption
        {
            Padding = "\0\0\0"u8.ToArray()
        };
        
        Assert.AreEqual(EdnsOptionType.Padding, expected.Type);
        
        opt1.Options.Add(expected);
        var opt2 = (OPTRecord)new ResourceRecord().Read(opt1.ToByteArray());
        var actual = (EdnsPaddingOption)opt2.Options[0];
        
        Assert.AreEqual(expected.Type, actual.Type);
        CollectionAssert.AreEqual(expected.Padding, actual.Padding);
    }
}