using System.Net;

using Makaretu.Dns;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsTests;

[TestClass]
public class AddressRecordTest
{
    [TestMethod]
    public void Create()
    {
        var rr = AddressRecord.Create("foo", IPAddress.Loopback);
        Assert.AreEqual("foo", rr.Name);
        Assert.AreEqual(DnsType.A, rr.Type);
        Assert.AreEqual(IPAddress.Loopback, rr.Address);

        rr = AddressRecord.Create("foo", IPAddress.IPv6Loopback);
        Assert.AreEqual("foo", rr.Name);
        Assert.AreEqual(DnsType.AAAA, rr.Type);
        Assert.AreEqual(IPAddress.IPv6Loopback, rr.Address);
    }
}