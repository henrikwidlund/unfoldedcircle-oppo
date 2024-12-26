using System;
using System.Threading.Tasks;

using Makaretu.Dns;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

namespace Makaretu.Mdns;

[TestClass]
public class RecentMessagesTest
{
    [TestMethod]
    public void Pruning()
    {
        var now = DateTimeOffset.UtcNow;
        var timeProviderMock = new Mock<TimeProvider>();
        timeProviderMock.Setup(static tp => tp.GetUtcNow()).Returns(now.AddSeconds(-2));
        timeProviderMock.Setup(static tp => tp.LocalTimeZone).Returns(TimeZoneInfo.Local);

        var messages = new RecentMessages(timeProviderMock.Object);
        messages.TryAdd("a"u8.ToArray());
        messages.TryAdd("b"u8.ToArray());
        timeProviderMock.Setup(static tp => tp.GetUtcNow()).Returns(now);
        byte[] cMessage = "c"u8.ToArray();
        messages.TryAdd(cMessage);
        
        Assert.AreEqual(1, messages.Count);
        Assert.IsTrue(messages.HasMessage(cMessage));
    }

    [TestMethod]
    public void MessageId()
    {
        var a0 = RecentMessages.GetId([1]);
        var a1 = RecentMessages.GetId([1]);
        var b = RecentMessages.GetId([2]);
        
        Assert.AreEqual(a0, a1);
        Assert.AreNotEqual(b, a0);
    }

    [TestMethod]
    public async Task DuplicateCheck()
    {
        var r = new RecentMessages { Interval = TimeSpan.FromMilliseconds(100) };
        var a = new byte[] { 1 };
        var b = new byte[] { 2 };

        Assert.IsTrue(r.TryAdd(a));
        Assert.IsTrue(r.TryAdd(b));
        Assert.IsFalse(r.TryAdd(a));

        await Task.Delay(200);
        Assert.IsTrue(r.TryAdd(a));
    }
}