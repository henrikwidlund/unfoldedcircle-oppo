using Makaretu.Dns;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsTests;

[TestClass]
public class ResourceRegistryTest
{
    [TestMethod]
    public void Exists() => Assert.AreNotEqual(0, ResourceRegistry.Records.Count);

    [TestMethod]
    public void Create()
    {
        var rr = ResourceRegistry.Create(DnsType.NS);
        Assert.IsInstanceOfType<NSRecord>(rr);

        rr = ResourceRegistry.Create((DnsType)1234);
        Assert.IsInstanceOfType<UnknownRecord>(rr);
    }
}