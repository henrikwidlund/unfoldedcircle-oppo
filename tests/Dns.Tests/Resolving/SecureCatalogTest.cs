﻿using System.IO;

using Makaretu.Dns;
using Makaretu.Dns.Resolving;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsTests.Resolving;

[TestClass]
public class SecureCatalogTest
{
    // From RFC 4035 Appendix A
    public const string ExampleZoneText = """
                                         example.       3600 IN SOA ns1.example. bugs.x.w.example. (
                                                                       1081539377
                                                                       3600
                                                                       300
                                                                       3600000
                                                                       3600
                                                                       )
                                                          3600 RRSIG  SOA 5 1 3600 20040509183619 (
                                                                       20040409183619 38519 example.
                                                                       ONx0k36rcjaxYtcNgq6iQnpNV5+drqYAsC9h
                                                                       7TSJaHCqbhE67Sr6aH2xDUGcqQWu/n0UVzrF
                                                                       vkgO9ebarZ0GWDKcuwlM6eNB5SiX2K74l5LW
                                                                       DA7S/Un/IbtDq4Ay8NMNLQI7Dw7n4p8/rjkB
                                                                       jV7j86HyQgM5e7+miRAz8V01b0I= )
                                                           3600 NS     ns1.example.
                                                           3600 NS     ns2.example.
                                                           3600 RRSIG  NS 5 1 3600 20040509183619 (
                                                                       20040409183619 38519 example.
                                                                       gl13F00f2U0R+SWiXXLHwsMY+qStYy5k6zfd
                                                                       EuivWc+wd1fmbNCyql0Tk7lHTX6UOxc8AgNf
                                                                       4ISFve8XqF4q+o9qlnqIzmppU3LiNeKT4FZ8
                                                                       RO5urFOvoMRTbQxW3U0hXWuggE4g3ZpsHv48
                                                                       0HjMeRaZB/FRPGfJPajngcq6Kwg= )
                                                           3600 MX     1 xx.example.
                                                           3600 RRSIG  MX 5 1 3600 20040509183619 (
                                                                       20040409183619 38519 example.
                                                                       HyDHYVT5KHSZ7HtO/vypumPmSZQrcOP3tzWB
                                                                       2qaKkHVPfau/DgLgS/IKENkYOGL95G4N+NzE
                                                                       VyNU8dcTOckT+ChPcGeVjguQ7a3Ao9Z/ZkUO
                                                                       6gmmUW4b89rz1PUxW4jzUxj66PTwoVtUU/iM
                                                                       W6OISukd1EQt7a0kygkg+PEDxdI= )
                                                           3600 NSEC   a.example. NS SOA MX RRSIG NSEC DNSKEY
                                                           3600 RRSIG  NSEC 5 1 3600 20040509183619 (
                                                                       20040409183619 38519 example.
                                                                       O0k558jHhyrC97ISHnislm4kLMW48C7U7cBm
                                                                       FTfhke5iVqNRVTB1STLMpgpbDIC9hcryoO0V
                                                                       Z9ME5xPzUEhbvGnHd5sfzgFVeGxr5Nyyq4tW
                                                                       SDBgIBiLQUv1ivy29vhXy7WgR62dPrZ0PWvm
                                                                       jfFJ5arXf4nPxp/kEowGgBRzY/U= )
                                                           3600 DNSKEY 256 3 5 (
                                                                       AQOy1bZVvpPqhg4j7EJoM9rI3ZmyEx2OzDBV
                                                                       rZy/lvI5CQePxXHZS4i8dANH4DX3tbHol61e
                                                                       k8EFMcsGXxKciJFHyhl94C+NwILQdzsUlSFo
                                                                       vBZsyl/NX6yEbtw/xN9ZNcrbYvgjjZ/UVPZI
                                                                       ySFNsgEYvh0z2542lzMKR4Dh8uZffQ==
                                                                       )
                                                           3600 DNSKEY 257 3 5 (
                                                                       AQOeX7+baTmvpVHb2CcLnL1dMRWbuscRvHXl
                                                                       LnXwDzvqp4tZVKp1sZMepFb8MvxhhW3y/0QZ
                                                                       syCjczGJ1qk8vJe52iOhInKROVLRwxGpMfzP
                                                                       RLMlGybr51bOV/1se0ODacj3DomyB4QB5gKT
                                                                       Yot/K9alk5/j8vfd4jWCWD+E1Sze0Q==
                                                                       )
                                                           3600 RRSIG  DNSKEY 5 1 3600 20040509183619 (
                                                                       20040409183619 9465 example.
                                                                       ZxgauAuIj+k1YoVEOSlZfx41fcmKzTFHoweZ
                                                                       xYnz99JVQZJ33wFS0Q0jcP7VXKkaElXk9nYJ
                                                                       XevO/7nAbo88iWsMkSpSR6jWzYYKwfrBI/L9
                                                                       hjYmyVO9m6FjQ7uwM4dCP/bIuV/DKqOAK9NY
                                                                       NC3AHfvCV1Tp4VKDqxqG7R5tTVM= )
                                                           3600 RRSIG  DNSKEY 5 1 3600 20040509183619 (
                                                                       20040409183619 38519 example.
                                                                       eGL0s90glUqcOmloo/2y+bSzyEfKVOQViD9Z
                                                                       DNhLz/Yn9CQZlDVRJffACQDAUhXpU/oP34ri
                                                                       bKBpysRXosczFrKqS5Oa0bzMOfXCXup9qHAp
                                                                       eFIku28Vqfr8Nt7cigZLxjK+u0Ws/4lIRjKk
                                                                       7z5OXogYVaFzHKillDt3HRxHIZM= )
                                         a.example.     3600 IN NS  ns1.a.example.
                                                           3600 IN NS  ns2.a.example.
                                                           3600 DS     57855 5 1 (
                                                                       B6DCD485719ADCA18E5F3D48A2331627FDD3
                                                                       636B )
                                                           3600 RRSIG  DS 5 2 3600 20040509183619 (
                                                                       20040409183619 38519 example.
                                                                       oXIKit/QtdG64J/CB+Gi8dOvnwRvqrto1AdQ
                                                                       oRkAN15FP3iZ7suB7gvTBmXzCjL7XUgQVcoH
                                                                       kdhyCuzp8W9qJHgRUSwKKkczSyuL64nhgjuD
                                                                       EML8l9wlWVsl7PR2VnZduM9bLyBhaaPmRKX/
                                                                       Fm+v6ccF2EGNLRiY08kdkz+XHHo= )
                                                           3600 NSEC   ai.example. NS DS RRSIG NSEC
                                                           3600 RRSIG  NSEC 5 2 3600 20040509183619 (
                                                                       20040409183619 38519 example.
                                                                       cOlYgqJLqlRqmBQ3iap2SyIsK4O5aqpKSoba
                                                                       U9fQ5SMApZmHfq3AgLflkrkXRXvgxTQSKkG2
                                                                       039/cRUs6Jk/25+fi7Xr5nOVJsb0lq4zsB3I
                                                                       BBdjyGDAHE0F5ROJj87996vJupdm1fbH481g
                                                                       sdkOW6Zyqtz3Zos8N0BBkEx+2G4= )
                                         ns1.a.example. 3600 IN A   192.0.2.5
                                         ns2.a.example. 3600 IN A   192.0.2.6
                                         ai.example.    3600 IN A   192.0.2.9
                                                        3600 RRSIG  A 5 2 3600 20040509183619 (
                                                                       20040409183619 38519 example.
                                                                       pAOtzLP2MU0tDJUwHOKE5FPIIHmdYsCgTb5B
                                                                       ERGgpnJluA9ixOyf6xxVCgrEJW0WNZSsJicd
                                                                       hBHXfDmAGKUajUUlYSAH8tS4ZnrhyymIvk3u
                                                                       ArDu2wfT130e9UHnumaHHMpUTosKe22PblOy
                                                                       6zrTpg9FkS0XGVmYRvOTNYx2HvQ= )
                                                        3600 HINFO  KLH-10 ITS
                                                        3600 RRSIG HINFO 5 2 3600 20040509183619 (
                                                                       20040409183619 38519 example.
                                                                       Iq/RGCbBdKzcYzlGE4ovbr5YcB+ezxbZ9W0l
                                                                       e/7WqyvhOO9J16HxhhL7VY/IKmTUY0GGdcfh
                                                                       ZEOCkf4lEykZF9NPok1/R/fWrtzNp8jobuY7
                                                                       AZEcZadp1WdDF3jc2/ndCa5XZhLKD3JzOsBw
                                                                       FvL8sqlS5QS6FY/ijFEDnI4RkZA= )
                                                         3600 AAAA   2001:db8::f00:baa9
                                                         3600 RRSIG AAAA 5 2 3600 20040509183619 (
                                                                       20040409183619 38519 example.
                                                                       nLcpFuXdT35AcE+EoafOUkl69KB+/e56XmFK
                                                                       kewXG2IadYLKAOBIoR5+VoQV3XgTcofTJNsh
                                                                       1rnF6Eav2zpZB3byI6yo2bwY8MNkr4A7cL9T
                                                                       cMmDwV/hWFKsbGBsj8xSCN/caEL2CWY/5XP2
                                                                       sZM6QjBBLmukH30+w1z3h8PUP2o= )
                                                         3600 NSEC b.example. A HINFO AAAA RRSIG NSEC
                                                         3600 RRSIG NSEC 5 2 3600 20040509183619 (
                                                                       20040409183619 38519 example.
                                                                       QoshyPevLcJ/xcRpEtMft1uoIrcrieVcc9pG
                                                                       CScIn5Glnib40T6ayVOimXwdSTZ/8ISXGj4p
                                                                       P8Sh0PlA6olZQ84L453/BUqB8BpdOGky4hsN
                                                                       3AGcLEv1Gr0QMvirQaFcjzOECfnGyBm+wpFL
                                                                       AhS+JOVfDI/79QtyTI0SaDWcg8U= )
                                         b.example.     3600 IN NS  ns1.b.example.
                                                        3600 IN NS  ns2.b.example.
                                                        3600 NSEC ns1.example. NS RRSIG NSEC
                                                        3600 RRSIG NSEC 5 2 3600 20040509183619 (
                                                                       20040409183619 38519 example.
                                                                       GNuxHn844wfmUhPzGWKJCPY5ttEX/RfjDoOx
                                                                       9ueK1PtYkOWKOOdiJ/PJKCYB3hYX+858dDWS
                                                                       xb2qnV/LSTCNVBnkm6owOpysY97MVj5VQEWs
                                                                       0lm9tFoqjcptQkmQKYPrwUnCSNwvvclSF1xZ
                                                                       vhRXgWT7OuFXldoCG6TfVFMs9xE = )
                                         ns1.b.example. 3600 IN A   192.0.2.7
                                         ns2.b.example. 3600 IN A   192.0.2.8
                                         ns1.example.   3600 IN A   192.0.2.1
                                                        3600 RRSIG A 5 2 3600 20040509183619 (
                                                                       20040409183619 38519 example.
                                                                       F1C9HVhIcs10cZU09G5yIVfKJy5yRQQ3qVet
                                                                       5pGhp82pzhAOMZ3K22JnmK4c+IjUeFp/to06
                                                                       im5FVpHtbFisdjyPq84bhTv8vrXt5AB1wNB+
                                                                       +iAqvIfdgW4sFNC6oADb1hK8QNauw9VePJhK
                                                                       v/iVXSYC0b7mPSU+EOlknFpVECs= )
                                                        3600 NSEC ns2.example. A RRSIG NSEC
                                                        3600 RRSIG NSEC 5 2 3600 20040509183619 (
                                                                       20040409183619 38519 example.
                                                                       I4hj+Kt6+8rCcHcUdolks2S+Wzri9h3fHas8
                                                                       1rGN/eILdJHN7JpV6lLGPIh/8fIBkfvdyWnB
                                                                       jjf1q3O7JgYO1UdI7FvBNWqaaEPJK3UkddBq
                                                                       ZIaLi8Qr2XHkjq38BeQsbp8X0+6h4ETWSGT8
                                                                       IZaIGBLryQWGLw6Y6X8dqhlnxJM = )
                                         ns2.example.   3600 IN A   192.0.2.2
                                                        3600 RRSIG A 5 2 3600 20040509183619 (
                                                                       20040409183619 38519 example.
                                                                       V7cQRw1TR+knlaL1z/psxlS1PcD37JJDaCMq
                                                                       Qo6/u1qFQu6x+wuDHRH22Ap9ulJPQjFwMKOu
                                                                       yfPGQPC8KzGdE3vt5snFEAoE1Vn3mQqtu7SO
                                                                       6amIjk13Kj/jyJ4nGmdRIc/3cM3ipXFhNTKq
                                                                       rdhx8SZ0yy4ObIRzIzvBFLiSS8o = )
                                                        3600 NSEC  *.w.example. A RRSIG NSEC
                                                        3600 RRSIG NSEC 5 2 3600 20040509183619 (
                                                                       20040409183619 38519 example.
                                                                       N0QzHvaJf5NRw1rE9uxS1Ltb2LZ73Qb9bKGE
                                                                       VyaISkqzGpP3jYJXZJPVTq4UVEsgT3CgeHvb
                                                                       3QbeJ5Dfb2V9NGCHj/OvF/LBxFFWwhLwzngH
                                                                       l+bQAgAcMsLu/nL3nDi1y/JSQjAcdZNDl4bw
                                                                       Ymx28EtgIpo9A0qmP08rMBqs1Jw = )
                                         *.w.example.   3600 IN MX  1 ai.example.
                                                        3600 RRSIG MX 5 2 3600 20040509183619 (
                                                                       20040409183619 38519 example.
                                                                       OMK8rAZlepfzLWW75Dxd63jy2wswESzxDKG2
                                                                       f9AMN1CytCd10cYISAxfAdvXSZ7xujKAtPbc
                                                                       tvOQ2ofO7AZJ+d01EeeQTVBPq4/6KCWhqe2X
                                                                       TjnkVLNvvhnc0u28aoSsG0+4InvkkOHknKxw
                                                                       4kX18MMR34i8lC36SR5xBni8vHI= )
                                                        3600 NSEC x.w.example.MX RRSIG NSEC
                                                        3600 RRSIG NSEC 5 2 3600 20040509183619 (
                                                                       20040409183619 38519 example.
                                                                       r/mZnRC3I/VIcrelgIcteSxDhtsdlTDt8ng9
                                                                       HSBlABOlzLxQtfgTnn8f+aOwJIAFe1Ee5RvU
                                                                       5cVhQJNP5XpXMJHfyps8tVvfxSAXfahpYqtx
                                                                       91gsmcV/1V9/bZAG55CefP9cM4Z9Y9NT9XQ8
                                                                       s1InQ2UoIv6tJEaaKkP701j8OLA = )
                                         x.w.example.   3600 IN MX  1 xx.example.
                                                        3600 RRSIG MX 5 3 3600 20040509183619 (
                                                                       20040409183619 38519 example.
                                                                       Il2WTZ+Bkv+OytBx4LItNW5mjB4RCwhOO8y1
                                                                       XzPHZmZUTVYL7LaA63f6T9ysVBzJRI3KRjAP
                                                                       H3U1qaYnDoN1DrWqmi9RJe4FoObkbcdm7P3I
                                                                       kx70ePCoFgRz1Yq+bVVXCvGuAU4xALv3W/Y1
                                                                       jNSlwZ2mSWKHfxFQxPtLj8s32+k= )
                                                        3600 NSEC x.y.w.example. MX RRSIG NSEC
                                                        3600 RRSIG NSEC 5 3 3600 20040509183619 (
                                                                       20040409183619 38519 example.
                                                                       aRbpHftxggzgMXdDlym9SsADqMZovZZl2QWK
                                                                       vw8J0tZEUNQByH5Qfnf5N1FqH/pS46UA7A4E
                                                                       mcWBN9PUA1pdPY6RVeaRlZlCr1IkVctvbtaI
                                                                       NJuBba/VHm+pebTbKcAPIvL9tBOoh+to1h6e
                                                                       IjgiM8PXkBQtxPq37wDKALkyn7Q = )
                                         x.y.w.example. 3600 IN MX  1 xx.example.
                                                        3600 RRSIG MX 5 4 3600 20040509183619 (
                                                                       20040409183619 38519 example.
                                                                       k2bJHbwP5LH5qN4is39UiPzjAWYmJA38Hhia
                                                                       t7i9t7nbX/e0FPnvDSQXzcK7UL+zrVA+3MDj
                                                                       q1ub4q3SZgcbLMgexxIW3Va//LVrxkP6Xupq
                                                                       GtOB9prkK54QTl/qZTXfMQpW480YOvVknhvb
                                                                       +gLcMZBnHJ326nb/TOOmrqNmQQE= )
                                                        3600 NSEC xx.example. MX RRSIG NSEC
                                                        3600 RRSIG NSEC 5 4 3600 20040509183619 (
                                                                       20040409183619 38519 example.
                                                                       OvE6WUzN2ziieJcvKPWbCAyXyP6ef8cr6Csp
                                                                       ArVSTzKSquNwbezZmkU7E34o5lmb6CWSSSpg
                                                                       xw098kNUFnHcQf/LzY2zqRomubrNQhJTiDTX
                                                                       a0ArunJQCzPjOYq5t0SLjm6qp6McJI1AP5Vr
                                                                       QoKqJDCLnoAlcPOPKAm/jJkn3jk= )
                                         xx.example.    3600 IN A   192.0.2.10
                                                        3600 RRSIG A 5 2 3600 20040509183619 (
                                                                       20040409183619 38519 example.
                                                                       kBF4YxMGWF0D8r0cztL+2fWWOvN1U/GYSpYP
                                                                       7SoKoNQ4fZKyk+weWGlKLIUM+uE1zjVTPXoa
                                                                       0Z6WG0oZp46rkl1EzMcdMgoaeUzzAJ2BMq+Y
                                                                       VdxG9IK1yZkYGY9AgbTOGPoAgbJyO9EPULsx
                                                                       kbIDV6GPPSZVusnZU6OMgdgzHV4= )
                                                        3600 HINFO  KLH-10 TOPS-20
                                                        3600 RRSIG HINFO 5 2 3600 20040509183619 (
                                                                       20040409183619 38519 example.
                                                                       GY2PLSXmMHkWHfLdggiox8+chWpeMNJLkML0
                                                                       t+U/SXSUsoUdR91KNdNUkTDWamwcF8oFRjhq
                                                                       BcPZ6EqrF+vl5v5oGuvSF7U52epfVTC+wWF8
                                                                       3yCUeUw8YklhLWlvk8gQ15YKth0ITQy8/wI+
                                                                       RgNvuwbioFSEuv2pNlkq0goYxNY= )
                                                        3600 AAAA   2001:db8::f00:baaa
                                                        3600 RRSIG AAAA 5 2 3600 20040509183619 (
                                                                       20040409183619 38519 example.
                                                                       Zzj0yodDxcBLnnOIwDsuKo5WqiaK24DlKg9C
                                                                       aGaxDFiKgKobUj2jilYQHpGFn2poFRetZd4z
                                                                       ulyQkssz2QHrVrPuTMS22knudCiwP4LWpVTr
                                                                       U4zfeA+rDz9stmSBP/4PekH/x2IoAYnwctd/
                                                                       xS9cL2QgW7FChw16mzlkH6/vsfs= )
                                                        3600 NSEC example. A HINFO AAAA RRSIG NSEC
                                                        3600 RRSIG NSEC 5 2 3600 20040509183619 (
                                                                       20040409183619 38519 example.
                                                                       ZFWUln6Avc8bmGl5GFjD3BwT530DUZKHNuoY
                                                                       9A8lgXYyrxu+pqgFiRVbyZRQvVB5pccEOT3k
                                                                       mvHgEa/HzbDB4PIYY79W+VHrgOxzdQGGCZzi
                                                                       asXrpSGOWwSOElghPnMIi8xdF7qtCntr382W
                                                                       GghLahumFIpg4MO3LS/prgzVVWo= )
                                         """;
    
    [TestMethod]
    public void IncludeZone()
    {
        var catalog = new Catalog();
        var reader = new PresentationReader(new StringReader(ExampleZoneText));
        var zone = catalog.IncludeZone(reader);
        Assert.AreEqual("example", zone.Name);
        Assert.IsTrue(zone.Authoritative);

        Assert.IsTrue(catalog.ContainsKey("example"));
        Assert.IsTrue(catalog.ContainsKey("ns1.example"));
        Assert.IsTrue(catalog.ContainsKey("xx.example"));
    }
}