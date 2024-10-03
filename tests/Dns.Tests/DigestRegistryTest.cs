using Makaretu.Dns;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsTests;

[TestClass]
public class DigestRegistryTest
{
    [TestMethod]
    public void Exists()
    {
        Assert.AreNotEqual(0, DigestRegistry.Digests.Count);
    }
}