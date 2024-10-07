using System;
using System.Linq;
using System.Net;

using Makaretu.Dns;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsTests;

[TestClass]
public class UpdatePrerequisiteListTest
{
    [TestMethod]
    public void MustExist_Name()
    {
        var prerequisites = new UpdatePrerequisiteList()
            .MustExist("www.example.org");
        var p = prerequisites[0];
        
        Assert.IsNotNull(p);
        Assert.AreEqual(DnsClass.ANY, p.Class);
        Assert.AreEqual("www.example.org", p.Name);
        Assert.AreEqual(TimeSpan.Zero, p.TTL);
        Assert.AreEqual(DnsType.ANY, p.Type);
        Assert.AreEqual(0, p.GetDataLength());
    }

    [TestMethod]
    public void MustExist_Name_Type()
    {
        var prerequisites = new UpdatePrerequisiteList()
            .MustExist("www.example.org", DnsType.A);
        var p = prerequisites[0];
        
        Assert.IsNotNull(p);
        Assert.AreEqual(DnsClass.ANY, p.Class);
        Assert.AreEqual("www.example.org", p.Name);
        Assert.AreEqual(TimeSpan.Zero, p.TTL);
        Assert.AreEqual(DnsType.A, p.Type);
        Assert.AreEqual(0, p.GetDataLength());
    }

    [TestMethod]
    public void MustExist_Name_Typename()
    {
        var prerequisites = new UpdatePrerequisiteList()
            .MustExist<ARecord>("www.example.org");
        var p = prerequisites[0];
        
        Assert.IsNotNull(p);
        Assert.AreEqual(DnsClass.ANY, p.Class);
        Assert.AreEqual("www.example.org", p.Name);
        Assert.AreEqual(TimeSpan.Zero, p.TTL);
        Assert.AreEqual(DnsType.A, p.Type);
        Assert.AreEqual(0, p.GetDataLength());
    }

    [TestMethod]
    public void MustExist_ResourceRecord()
    {
        var rr = new ARecord
        {
            Name = "local",
            Class = DnsClass.IN,
            Address = IPAddress.Parse("127.0.0.0")
        };
        
        var prerequisites = new UpdatePrerequisiteList()
            .MustExist(rr);
        var p = prerequisites[0];
        
        Assert.IsNotNull(p);
        Assert.AreEqual(rr.Class, p.Class);
        Assert.AreEqual(rr.Name, p.Name);
        Assert.AreEqual(TimeSpan.Zero, p.TTL);
        Assert.AreEqual(rr.Type, p.Type);
        Assert.AreEqual(rr.GetDataLength(), p.GetDataLength());
        Assert.IsTrue(rr.GetData().SequenceEqual(p.GetData()));
    }

    [TestMethod]
    public void MustNotExist_Name()
    {
        var prerequisites = new UpdatePrerequisiteList()
            .MustNotExist("www.example.org");
        var p = prerequisites[0];
        
        Assert.IsNotNull(p);
        Assert.AreEqual(DnsClass.None, p.Class);
        Assert.AreEqual("www.example.org", p.Name);
        Assert.AreEqual(TimeSpan.Zero, p.TTL);
        Assert.AreEqual(DnsType.ANY, p.Type);
        Assert.AreEqual(0, p.GetDataLength());
    }

    [TestMethod]
    public void MustNotExist_Name_Type()
    {
        var prerequisites = new UpdatePrerequisiteList()
            .MustNotExist("www.example.org", DnsType.A);
        var p = prerequisites[0];
        
        Assert.IsNotNull(p);
        Assert.AreEqual(DnsClass.None, p.Class);
        Assert.AreEqual("www.example.org", p.Name);
        Assert.AreEqual(TimeSpan.Zero, p.TTL);
        Assert.AreEqual(DnsType.A, p.Type);
        Assert.AreEqual(0, p.GetDataLength());
    }

    [TestMethod]
    public void MustNotExist_Name_Typename()
    {
        var prerequisites = new UpdatePrerequisiteList()
            .MustNotExist<ARecord>("www.example.org");
        var p = prerequisites[0];
        
        Assert.IsNotNull(p);
        Assert.AreEqual(DnsClass.None, p.Class);
        Assert.AreEqual("www.example.org", p.Name);
        Assert.AreEqual(TimeSpan.Zero, p.TTL);
        Assert.AreEqual(DnsType.A, p.Type);
        Assert.AreEqual(0, p.GetDataLength());
    }

}