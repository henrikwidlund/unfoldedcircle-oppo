using System;
using System.Threading.Tasks;

using Makaretu.Dns;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Makaretu.Mdns;

[TestClass]
public class RecentMessagesTest
{
    [TestMethod]
    public void Pruning()
    {
        var messages = new RecentMessages();
        var now = DateTime.Now;
        messages.Messages.TryAdd("a", now.AddSeconds(-2));
        messages.Messages.TryAdd("b", now.AddSeconds(-3));
        messages.Messages.TryAdd("c", now);
        
        Assert.AreEqual(2, messages.Prune());
        Assert.AreEqual(1, messages.Messages.Count);
        Assert.IsTrue(messages.Messages.ContainsKey("c"));
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