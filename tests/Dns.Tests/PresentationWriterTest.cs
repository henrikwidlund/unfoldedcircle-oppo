﻿using System;
using System.IO;
using System.Net;

using Makaretu.Dns;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsTests;

[TestClass]
public class PresentationWriterTest
{
    [TestMethod]
    public void WriteByte()
    {
        using var text = new StringWriter();
        var writer = new PresentationWriter(text);
        writer.WriteByte(byte.MaxValue);
        writer.WriteByte(1, appendSpace: false);
        
        Assert.AreEqual("255 1", text.ToString());
    }

    [TestMethod]
    public void WriteUInt16()
    {
        using var text = new StringWriter();
        var writer = new PresentationWriter(text);
        writer.WriteUInt16(ushort.MaxValue);
        writer.WriteUInt16(1, appendSpace: false);
        
        Assert.AreEqual("65535 1", text.ToString());
    }

    [TestMethod]
    public void WriteUInt32()
    {
        using var text = new StringWriter();
        var writer = new PresentationWriter(text);
        writer.WriteUInt32(int.MaxValue);
        writer.WriteUInt32(1, appendSpace: false);
        
        Assert.AreEqual("2147483647 1", text.ToString());
    }

    [TestMethod]
    public void WriteString()
    {
        using var text = new StringWriter();
        var writer = new PresentationWriter(text);
        writer.WriteString("alpha");
        writer.WriteString("a b");
        writer.WriteString(null);
        writer.WriteString("");
        writer.WriteString(" ");
        writer.WriteString("a\\b");
        writer.WriteString("a\"b");
        writer.WriteString("end", appendSpace: false);
        
        Assert.AreEqual("alpha \"a b\" \"\" \"\" \" \" a\\\\b a\\\"b end", text.ToString());
    }

    [TestMethod]
    public void WriteStringUnencoded()
    {
        using var text = new StringWriter();
        var writer = new PresentationWriter(text);
        writer.WriteStringUnencoded("\\a");
        writer.WriteStringUnencoded("\\b", appendSpace: false);
        
        Assert.AreEqual(@"\a \b", text.ToString());
    }

    [TestMethod]
    public void WriteDomainName()
    {
        using var text1 = new StringWriter();
        var writer = new PresentationWriter(text1);
        writer.WriteString("alpha.com");
        writer.WriteString("omega.com", appendSpace: false);
        Assert.AreEqual("alpha.com omega.com", text1.ToString());

        using var text2 = new StringWriter();
        writer = new PresentationWriter(text2);
        writer.WriteDomainName(new DomainName("alpha.com"), false);
        Assert.AreEqual("alpha.com", text2.ToString());
    }

    [TestMethod]
    public void WriteDomainName_Escaped()
    {
        using var text = new StringWriter();
        var writer = new PresentationWriter(text);
        writer.WriteDomainName(new DomainName(@"dr\. smith.com"), false);
        
        Assert.AreEqual(@"dr\.\032smith.com", text.ToString());
    }

    [TestMethod]
    public void WriteBase16String()
    {
        using var text = new StringWriter();
        var writer = new PresentationWriter(text);
        writer.WriteBase16String([1, 2, 3]);
        writer.WriteBase16String([1, 2, 3], appendSpace: false);
        
        Assert.AreEqual("010203 010203", text.ToString());
    }

    [TestMethod]
    public void WriteBase64String()
    {
        using var text = new StringWriter();
        var writer = new PresentationWriter(text);
        writer.WriteBase64String([1, 2, 3]);
        writer.WriteBase64String([1, 2, 3], appendSpace: false);
        
        Assert.AreEqual("AQID AQID", text.ToString());
    }

    [TestMethod]
    public void WriteTimeSpan16()
    {
        using var text = new StringWriter();
        var writer = new PresentationWriter(text);
        writer.WriteTimeSpan16(TimeSpan.FromSeconds(ushort.MaxValue));
        writer.WriteTimeSpan16(TimeSpan.Zero, appendSpace: false);
        
        Assert.AreEqual("65535 0", text.ToString());
    }

    [TestMethod]
    public void WriteTimeSpan32()
    {
        using var text = new StringWriter();
        var writer = new PresentationWriter(text);
        writer.WriteTimeSpan32(TimeSpan.FromSeconds(int.MaxValue));
        writer.WriteTimeSpan32(TimeSpan.Zero, appendSpace: false);
        
        Assert.AreEqual("2147483647 0", text.ToString());
    }

    [TestMethod]
    public void WriteDateTime()
    {
        using var text = new StringWriter();
        var writer = new PresentationWriter(text);
        writer.WriteDateTime(DateTime.UnixEpoch);
        writer.WriteDateTime(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc), appendSpace: false);
        
        Assert.AreEqual("19700101000000 99991231235959", text.ToString());
    }

    [TestMethod]
    public void WriteIPAddress()
    {
        using var text = new StringWriter();
        var writer = new PresentationWriter(text);
        writer.WriteIPAddress(IPAddress.Loopback);
        writer.WriteIPAddress(IPAddress.IPv6Loopback, appendSpace: false);
        
        Assert.AreEqual("127.0.0.1 ::1", text.ToString());
    }

    [TestMethod]
    public void WriteDnsType()
    {
        using var text = new StringWriter();
        var writer = new PresentationWriter(text);
        writer.WriteDnsType(DnsType.ANY);
        writer.WriteDnsType((DnsType)1234, appendSpace: false);
        
        Assert.AreEqual("ANY TYPE1234", text.ToString());
    }

    [TestMethod]
    public void WriteDnsClass()
    {
        using var text = new StringWriter();
        var writer = new PresentationWriter(text);
        writer.WriteDnsClass(DnsClass.IN);
        writer.WriteDnsClass((DnsClass)1234, appendSpace: false);
        
        Assert.AreEqual("IN CLASS1234", text.ToString());
    }
}