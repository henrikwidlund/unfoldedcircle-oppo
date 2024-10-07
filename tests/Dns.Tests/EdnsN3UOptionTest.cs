using System.Linq;

using Makaretu.Dns;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsTests;

[TestClass]
public class EdnsN3UOptionTest
{
    [TestMethod]
    public void Roundtrip()
    {
        var opt1 = new OPTRecord();
        var expected = new EdnsN3UOption
        {
            Algorithms = { DigestType.GostR34_11_94, DigestType.Sha512 }
        };
        
        Assert.AreEqual(EdnsOptionType.N3U, expected.Type);
        
        opt1.Options.Add(expected);
        var opt2 = (OPTRecord)new ResourceRecord().Read(opt1.ToByteArray());
        var actual = (EdnsN3UOption)opt2.Options[0];
        
        Assert.AreEqual(expected.Type, actual.Type);
        CollectionAssert.AreEqual(expected.Algorithms, actual.Algorithms);
    }

    [TestMethod]
    public void Create()
    {
        var option = EdnsN3UOption.Create();
        
        Assert.AreEqual(EdnsOptionType.N3U, option.Type);
        CollectionAssert.AreEqual(DigestRegistry.Digests.ToArray(), option.Algorithms);
    }
}