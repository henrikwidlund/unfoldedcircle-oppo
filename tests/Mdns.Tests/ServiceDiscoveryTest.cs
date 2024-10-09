using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Makaretu.Dns;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Makaretu.Mdns;

// ReSharper disable AccessToDisposedClosure
[TestClass]
public class ServiceDiscoveryTest
{
    [TestMethod]
    public async Task Disposable()
    {
        using (var sd = await ServiceDiscovery.CreateInstance())
            Assert.IsNotNull(sd);

        var mdns = new MulticastService();
        using (var sd = await ServiceDiscovery.CreateInstance(mdns))
            Assert.IsNotNull(sd);
    }

    [TestMethod]
    public async Task Advertises_Service()
    {
        var service = new ServiceProfile("x", "_sdtest-1._udp", 1024, [IPAddress.Loopback]);
        var done = new ManualResetEvent(false);

        var mdns = new MulticastService();
        mdns.NetworkInterfaceDiscovered += _ =>
            mdns.SendQuery(ServiceDiscovery.ServiceName, DnsClass.IN, DnsType.PTR);
        
        mdns.AnswerReceived += e =>
        {
            var msg = e.Message;
            if (msg.Answers.OfType<PTRRecord>().Any(p => p.DomainName == service.QualifiedServiceName && ((int)p.Class & MulticastService.CacheFlushBit) != 0))
                Assert.Fail("shared PTR records should not have cache-flush set");
            
            if (msg.Answers.OfType<PTRRecord>().Any(p => p.DomainName == service.QualifiedServiceName))
                done.Set();
            
            return Task.CompletedTask;
        };
        
        try
        {
            using var sd = await ServiceDiscovery.CreateInstance(mdns);
            sd.Advertise(service);
            await mdns.Start(CancellationToken.None);
            
            Assert.IsTrue(done.WaitOne(TimeSpan.FromSeconds(1)), "query timeout");
        }
        finally
        {
            mdns.Stop();
        }
    }

    [TestMethod]
    public async Task Advertises_SharedService()
    {
        var service = new ServiceProfile("x", "_sdtest-1._udp", 1024, [IPAddress.Loopback], true);
        var done = new ManualResetEvent(false);
        
        Assert.IsTrue(service.SharedProfile, "Shared Profile was not set");
        
        using var mdns = new MulticastService();
        mdns.NetworkInterfaceDiscovered += _ => mdns.SendQuery(service.QualifiedServiceName);
        mdns.AnswerReceived += e =>
        {
            var msg = e.Message;
            if (msg.Answers.OfType<PTRRecord>().Any(p => p.DomainName == service.QualifiedServiceName && ((int)p.Class & MulticastService.CacheFlushBit) != 0))
                Assert.Fail("shared PTR records should not have cache-flush set");
            
            if (msg.AdditionalRecords.OfType<SRVRecord>().Any(s => (s.Name == service.FullyQualifiedName && ((int)s.Class & MulticastService.CacheFlushBit) == 0)))
                done.Set();
            
            return Task.CompletedTask;
        };
        
        try
        {
            using var sd = await ServiceDiscovery.CreateInstance(mdns);
            sd.Advertise(service);
            await mdns.Start(CancellationToken.None);
            
            Assert.IsTrue(done.WaitOne(TimeSpan.FromSeconds(1)), "query timeout");
        }
        finally
        {
            mdns.Stop();
        }
    }

    [TestMethod]
    public async Task Probe_Service()
    {
        var service = new ServiceProfile("z", "_sdtest-11._udp", 1024, [IPAddress.Loopback]);
        var done = new ManualResetEvent(false);

        using var mdns = new MulticastService();
        using var sd = await ServiceDiscovery.CreateInstance(mdns);
        mdns.NetworkInterfaceDiscovered += async _ =>
            {
                if (await sd.Probe(service))
                    done.Set();
            };
        
        try
        {
            sd.Advertise(service);
            await mdns.Start(CancellationToken.None);
            
            Assert.IsTrue(done.WaitOne(TimeSpan.FromSeconds(3)), "Probe timeout");
        }
        finally
        {
            mdns.Stop();
        }
    }

    [TestMethod]
    public async Task Probe_Service2()
    {
        var service = new ServiceProfile("z", "_sdtest-11._udp", 1024, [IPAddress.Loopback]);

        using var sd = await ServiceDiscovery.CreateInstance();
        sd.Advertise(service);
        await sd.Mdns!.Start(CancellationToken.None);
        
        var mdns = new MulticastService();
        using var sd2 = await ServiceDiscovery.CreateInstance(mdns);
        mdns.NetworkInterfaceDiscovered += async _ =>
        {
            Assert.IsTrue(await sd2.Probe(service));
        };
        
        try
        {
            await mdns.Start(CancellationToken.None);
        }
        finally
        {
            mdns.Stop();
        }
    }

    [TestMethod]
    public async Task Probe_Service3()
    {
        var service = new ServiceProfile("z", "_sdtest-11._udp", 1024, [IPAddress.Loopback]);

        var mdns = new MulticastService();
        using var sd = await ServiceDiscovery.CreateInstance(mdns);
        mdns.NetworkInterfaceDiscovered += async _ =>
        {
            Assert.IsFalse(await sd.Probe(service));
        };
        
        try
        {
            await mdns.Start(CancellationToken.None);
        }
        finally
        {
            mdns.Stop();
        }
    }

    [TestMethod]
    public async Task Advertises_ServiceInstances()
    {
        var service = new ServiceProfile("x", "_sdtest-1._udp", 1024, [IPAddress.Loopback]);
        var done = new ManualResetEvent(false);

        using var mdns = new MulticastService();
        mdns.NetworkInterfaceDiscovered += _ => mdns.SendQuery(service.QualifiedServiceName, DnsClass.IN, DnsType.PTR);
        mdns.AnswerReceived += e =>
        {
            var msg = e.Message;
            if (msg.Answers.OfType<PTRRecord>().Any(p => p.DomainName == service.FullyQualifiedName))
                done.Set();
            
            return Task.CompletedTask;
        };
        
        try
        {
            using var sd = await ServiceDiscovery.CreateInstance(mdns);
            sd.Advertise(service);
            await mdns.Start(CancellationToken.None);
            
            Assert.IsTrue(done.WaitOne(TimeSpan.FromSeconds(1)), "query timeout");
        }
        finally
        {
            mdns.Stop();
        }
    }

    [TestMethod]
    public async Task Advertises_ServiceInstance_Address()
    {
        var service = new ServiceProfile("x2", "_sdtest-1._udp", 1024, [IPAddress.Loopback]);
        var done = new ManualResetEvent(false);

        using var mdns = new MulticastService();
        mdns.NetworkInterfaceDiscovered += _ => mdns.SendQuery(service.HostName, DnsClass.IN, DnsType.A);
        mdns.AnswerReceived += e =>
        {
            var msg = e.Message;
            if (msg.Answers.OfType<ARecord>().Any(p => p.Name == service.HostName))
                done.Set();
            
            return Task.CompletedTask;
        };
        try
        {
            using var sd = await ServiceDiscovery.CreateInstance(mdns);
            sd.Advertise(service);
            await mdns.Start(CancellationToken.None);
            
            Assert.IsTrue(done.WaitOne(TimeSpan.FromSeconds(1)), "query timeout");
        }
        finally
        {
            mdns.Stop();
        }
    }

    [TestMethod]
    public async Task Advertises_ServiceInstance_Subtype()
    {
        var service = new ServiceProfile("x2", "_sdtest-1._udp", 1024, [IPAddress.Loopback]);
        service.Subtypes.Add("_example");
        var done = new ManualResetEvent(false);

        using var mdns = new MulticastService();
        mdns.NetworkInterfaceDiscovered += _ => mdns.SendQuery("_example._sub._sdtest-1._udp.local", DnsClass.IN, DnsType.PTR);
        mdns.AnswerReceived += e =>
        {
            var msg = e.Message;
            if (msg.Answers.OfType<PTRRecord>().Any(p => p.DomainName == service.FullyQualifiedName))
                done.Set();
            
            return Task.CompletedTask;
        };
        
        try
        {
            using var sd = await ServiceDiscovery.CreateInstance(mdns);
            sd.Advertise(service);
            await mdns.Start(CancellationToken.None);
            
            Assert.IsTrue(done.WaitOne(TimeSpan.FromSeconds(1)), "query timeout");
        }
        finally
        {
            mdns.Stop();
        }
    }

    [TestMethod]
    public async Task Discover_AllServices()
    {
        var service = new ServiceProfile("x", "_sdtest-2._udp", 1024);
        var done = new ManualResetEvent(false);
        using var mdns = new MulticastService();
        using var sd = await ServiceDiscovery.CreateInstance(mdns);

        mdns.NetworkInterfaceDiscovered += _ => sd.QueryAllServices();
        sd.ServiceDiscovered += serviceName =>
        {
            if (serviceName == service.QualifiedServiceName)
                done.Set();
            
            return Task.CompletedTask;
        };
        
        try
        {
            sd.Advertise(service);
            await mdns.Start(CancellationToken.None);
            
            Assert.IsTrue(done.WaitOne(TimeSpan.FromSeconds(1)), "DNS-SD query timeout");
        }
        finally
        {
            mdns.Stop();
        }
    }

    [TestMethod]
    public async Task Discover_AllServices_Unicast()
    {
        var service = new ServiceProfile("x", "_sdtest-5._udp", 1024);
        var done = new ManualResetEvent(false);
        using var mdns = new MulticastService();
        using var sd = await ServiceDiscovery.CreateInstance(mdns);

        mdns.NetworkInterfaceDiscovered += _ => sd.QueryUnicastAllServices();
        sd.ServiceDiscovered += serviceName =>
        {
            if (serviceName == service.QualifiedServiceName)
                done.Set();
            
            return Task.CompletedTask;
        };
        try
        {
            sd.Advertise(service);
            await mdns.Start(CancellationToken.None);
            
            Assert.IsTrue(done.WaitOne(TimeSpan.FromSeconds(1)), "DNS-SD query timeout");
        }
        finally
        {
            mdns.Stop();
        }
    }

    [TestMethod]
    public async Task Discover_ServiceInstance()
    {
        var service = new ServiceProfile("y", "_sdtest-2._udp", 1024);
        var done = new ManualResetEvent(false);
        using var mdns = new MulticastService();
        using var sd = await ServiceDiscovery.CreateInstance(mdns);

        mdns.NetworkInterfaceDiscovered += async _ =>
        {
            await sd.QueryServiceInstances(service.ServiceName!);
        };

        sd.ServiceInstanceDiscovered += e =>
        {
            if (e.ServiceInstanceName == service.FullyQualifiedName)
            {
                Assert.IsNotNull(e.Message);
                done.Set();
            }
            
            return Task.CompletedTask;
        };
        
        try
        {
            sd.Advertise(service);
            await mdns.Start(CancellationToken.None);
            
            Assert.IsTrue(done.WaitOne(TimeSpan.FromSeconds(1)), "instance not found");
        }
        finally
        {
            mdns.Stop();
        }
    }

    [TestMethod]
    public async Task Discover_ServiceInstance_with_Subtype()
    {
        var service1 = new ServiceProfile("x", "_sdtest-2._udp", 1024);
        var service2 = new ServiceProfile("y", "_sdtest-2._udp", 1024);
        service2.Subtypes.Add("apiv2");
        var done = new ManualResetEvent(false);
        using var mdns = new MulticastService();
        using var sd = await ServiceDiscovery.CreateInstance(mdns);

        mdns.NetworkInterfaceDiscovered += async _ =>
        {
            await sd.QueryServiceInstances("_sdtest-2._udp", "apiv2");
        };

        sd.ServiceInstanceDiscovered += e =>
        {
            if (e.ServiceInstanceName == service2.FullyQualifiedName)
            {
                Assert.IsNotNull(e.Message);
                done.Set();
            }
            
            return Task.CompletedTask;
        };
        
        try
        {
            sd.Advertise(service1);
            sd.Advertise(service2);
            await mdns.Start(CancellationToken.None);
            
            Assert.IsTrue(done.WaitOne(TimeSpan.FromSeconds(1)), "instance not found");
        }
        finally
        {
            mdns.Stop();
        }
    }

    [TestMethod]
    public async Task Discover_ServiceInstance_Unicast()
    {
        var service = new ServiceProfile("y", "_sdtest-5._udp", 1024);
        var done = new ManualResetEvent(false);
        using var mdns = new MulticastService();
        using var sd = await ServiceDiscovery.CreateInstance(mdns);

        mdns.NetworkInterfaceDiscovered += async _ =>
        {
            await sd.QueryUnicastServiceInstances(service.ServiceName!);
        };

        sd.ServiceInstanceDiscovered += e =>
        {
            if (e.ServiceInstanceName == service.FullyQualifiedName)
            {
                Assert.IsNotNull(e.Message);
                done.Set();
            }
            
            return Task.CompletedTask;
        };
        
        try
        {
            sd.Advertise(service);
            await mdns.Start(CancellationToken.None);
            
            Assert.IsTrue(done.WaitOne(TimeSpan.FromSeconds(1)), "instance not found");
        }
        finally
        {
            mdns.Stop();
        }
    }

    [TestMethod]
    public async Task Discover_ServiceInstance_WithAnswersContainingAdditionRecords()
    {
        var service = new ServiceProfile("y", "_sdtest-2._udp", 1024, [IPAddress.Parse("127.1.1.1")]);
        var done = new ManualResetEvent(false);

        using var mdns = new MulticastService();
        using var sd = await ServiceDiscovery.CreateInstance(mdns);
        sd.AnswersContainsAdditionalRecords = true;
        
        Message discovered = null;

        mdns.NetworkInterfaceDiscovered += async _ =>
        {
            await sd.QueryServiceInstances(service.ServiceName!);
        };

        sd.ServiceInstanceDiscovered += e =>
        {
            if (e.ServiceInstanceName == service.FullyQualifiedName)
            {
                Assert.IsNotNull(e.Message);
                discovered = e.Message;
                done.Set();
            }
            
            return Task.CompletedTask;
        };

        sd.Advertise(service);

        await mdns.Start(CancellationToken.None);

        Assert.IsTrue(done.WaitOne(TimeSpan.FromSeconds(3)), "instance not found");

        const int additionalRecordsCount = 1 + // SRVRecord
                                           1 + // TXTRecord
                                           1; // AddressRecord

        const int answersCount = additionalRecordsCount +
                                 1; // PTRRecord

        Assert.AreEqual(0, discovered.AdditionalRecords.Count);
        Assert.AreEqual(answersCount, discovered.Answers.Count);
    }

    [TestMethod]
    public async Task Unadvertise()
    {
        var service = new ServiceProfile("z", "_sdtest-7._udp", 1024);
        var done = new ManualResetEvent(false);
        using var mdns = new MulticastService();
        using var sd = await ServiceDiscovery.CreateInstance(mdns);

        mdns.NetworkInterfaceDiscovered += _ => sd.QueryAllServices();
        sd.ServiceInstanceShutdown += e =>
        {
            if (e.ServiceInstanceName == service.FullyQualifiedName)
                done.Set();
            
            return Task.CompletedTask;
        };
        
        try
        {
            sd.Advertise(service);
            await mdns.Start(CancellationToken.None);
            await sd.Unadvertise(service);
            Assert.IsTrue(done.WaitOne(TimeSpan.FromSeconds(1)), "goodbye timeout");
        }
        finally
        {
            mdns.Stop();
        }
    }
    
    [TestMethod]
    public async Task ReverseAddressMapping()
    {
        var service = new ServiceProfile("x9", "_sdtest-1._udp", 1024, [IPAddress.Loopback, IPAddress.IPv6Loopback]);
        var arpaAddress = IPAddress.Loopback.GetArpaName();
        var done = new ManualResetEvent(false);

        using var mdns = new MulticastService();
        Message response = null;
        mdns.NetworkInterfaceDiscovered += _ => mdns.SendQuery(arpaAddress, DnsClass.IN, DnsType.PTR);
        mdns.AnswerReceived += e =>
        {
            var msg = e.Message;
            if (msg.Answers.OfType<PTRRecord>().Any(p => p.Name == arpaAddress))
            {
                response = msg;
                done.Set();
            }
            
            return Task.CompletedTask;
        };
        
        try
        {
            using var sd = await ServiceDiscovery.CreateInstance(mdns);
            sd.Advertise(service);
            await mdns.Start(CancellationToken.None);
            
            Assert.IsTrue(done.WaitOne(TimeSpan.FromSeconds(1)), "query timeout");
            
            var answers = response.Answers
                .OfType<PTRRecord>()
                .Where(ptr => service.HostName == ptr.DomainName);
            
            foreach (var answer in answers)
            {
                Assert.AreEqual(arpaAddress, answer.Name);
                Assert.IsTrue(answer.TTL > TimeSpan.Zero);
                Assert.AreEqual(DnsClass.IN, answer.Class);
            }
        }
        finally
        {
            mdns.Stop();
        }
    }

    [TestMethod]
    public async Task ResourceRecords()
    {
        var profile = new ServiceProfile("me", "_myservice._udp", 1234, [IPAddress.Loopback]);
        profile.Subtypes.Add("apiv2");
        profile.AddProperty("someprop", "somevalue");

        using var sd = await ServiceDiscovery.CreateInstance();
        sd.Advertise(profile);

        Assert.IsNotNull(sd.NameServer.Catalog);
        
        var resourceRecords = sd.NameServer.Catalog.Values.SelectMany(static node => node.Resources);
        foreach (var r in resourceRecords)
            Console.WriteLine(r.ToString());
    }

    [TestMethod]
    public async Task Announce_ContainsSharedRecords()
    {
        var service = new ServiceProfile("z", "_sdtest-4._udp", 1024, [IPAddress.Loopback]);
        var done = new ManualResetEvent(false);

        using var mdns = new MulticastService();
        mdns.AnswerReceived += e =>
        {
            var msg = e.Message;
            if (msg.Answers.OfType<PTRRecord>().Any(p => p.DomainName == service.FullyQualifiedName))
                done.Set();
            
            return Task.CompletedTask;
        };
        
        try
        {
            using var sd = await ServiceDiscovery.CreateInstance(mdns);
            mdns.NetworkInterfaceDiscovered += async _ =>
            {
                Assert.IsFalse(await sd.Probe(service));
                await sd.Announce(service);
            };
            
            await mdns.Start(CancellationToken.None);
            
            Assert.IsTrue(done.WaitOne(TimeSpan.FromSeconds(3)), "announce timeout");
        }
        finally
        {
            mdns.Stop();
        }
    }

    [TestMethod]
    public async Task Announce_ContainsResourceRecords()
    {
        var service = new ServiceProfile("z", "_sdtest-4._udp", 1024, [IPAddress.Loopback]);
        var done = new ManualResetEvent(false);

        using var mdns = new MulticastService();
        mdns.AnswerReceived += e =>
        {
            var msg = e.Message;
            //Remove Cache-Flush bit
            foreach (var answer in e.Message.Answers)
                answer.Class = (DnsClass)((ushort)answer.Class & ~MulticastService.CacheFlushBit);
            
            foreach (var r in service.Resources)
            {
                if (!msg.Answers.Contains(r))
                    return Task.CompletedTask;
            }
            
            done.Set();
            
            return Task.CompletedTask;
        };
        
        try
        {
            using var sd = await ServiceDiscovery.CreateInstance(mdns);
            mdns.NetworkInterfaceDiscovered += async _ =>
                {
                    Assert.IsFalse(await sd.Probe(service));
                    await sd.Announce(service);
                };
            
            await mdns.Start(CancellationToken.None);
            
            Assert.IsTrue(done.WaitOne(TimeSpan.FromSeconds(3)), "announce timeout");
        }
        finally
        {
            mdns.Stop();
        }
    }

    [TestMethod]
    public async Task Announce_SentThrice()
    {
        var service = new ServiceProfile("z", "_sdtest-4._udp", 1024, [IPAddress.Loopback]);
        var done = new ManualResetEvent(false);
        var nanswers = 0;
        DateTime start = DateTime.Now;
        
        using var mdns = new MulticastService
        {
            IgnoreDuplicateMessages = false
        };
        
        mdns.AnswerReceived += e =>
        {
            var msg = e.Message;
            if (msg.Answers.OfType<PTRRecord>().Any(p => p.DomainName == service.FullyQualifiedName))
            {
                if (++nanswers == 3)
                    done.Set();
            }
            
            return Task.CompletedTask;
        };
        
        try
        {
            using var sd = await ServiceDiscovery.CreateInstance(mdns);
            mdns.NetworkInterfaceDiscovered += async _ =>
            {
                Assert.IsFalse(await sd.Probe(service));
                start = DateTime.Now;
                await sd.Announce(service, 3);
            };
            
            await mdns.Start(CancellationToken.None);
            
            Assert.IsTrue(done.WaitOne(TimeSpan.FromSeconds(4)), "announce timeout");
            if ((DateTime.Now - start).TotalMilliseconds < 3000)
                Assert.Fail("Announcing too fast");
        }
        finally
        {
            mdns.Stop();
        }
    }
}