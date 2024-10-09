﻿using Makaretu.Dns;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsTests;

[TestClass]
public class QuestionTest
{
    [TestMethod]
    public void Roundtrip()
    {
        var a = new Question
        {
            Name = "emanon.org",
            Class = DnsClass.CH,
            Type = DnsType.MX
        };
        
        var b = (Question)new Question().Read(a.ToByteArray());
        
        Assert.AreEqual(a.Name, b.Name);
        Assert.AreEqual(a.Class, b.Class);
        Assert.AreEqual(a.Type, b.Type);
    }
}