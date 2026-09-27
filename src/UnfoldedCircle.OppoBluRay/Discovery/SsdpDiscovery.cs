using System.Collections.Frozen;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;

using Oppo;

namespace UnfoldedCircle.OppoBluRay.Discovery;

/// <summary>
/// Discovers Oppo players that expose a standard UPnP device description
/// (BDP-103/103D/105/105D and UDP-203/205 only).
/// </summary>
public sealed class SsdpDiscovery(IHttpClientFactory httpClientFactory) : IPlayerDiscovery
{
    private static readonly byte[] SearchMessage =
        Encoding.ASCII.GetBytes(
            "M-SEARCH * HTTP/1.1\r\n" +
            "HOST:239.255.255.250:1900\r\n" +
            "MAN:\"ssdp:discover\"\r\n" +
            "MX:2\r\n" +
            "ST:ssdp:all\r\n" +
            "\r\n");

    private const string OppoManufacturer = "OPPO";

    private static readonly FrozenDictionary<string, OppoModel> ModelsByUpnpName = new Dictionary<string, OppoModel>(StringComparer.OrdinalIgnoreCase)
    {
        ["OPPO BDP-103"] = OppoModel.BDP10X,
        ["OPPO BDP-103D"] = OppoModel.BDP10X,
        ["OPPO BDP-105"] = OppoModel.BDP10X,
        ["OPPO BDP-105D"] = OppoModel.BDP10X,
        ["OPPO UDP-203"] = OppoModel.UDP203,
        ["OPPO UDP-205"] = OppoModel.UDP205,
    }.ToFrozenDictionary();

    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();

    public string Name => "SSDP";

    public async IAsyncEnumerable<PlayerInfo> DiscoverAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var udp = new UdpClient();

        await udp.SendAsync(SearchMessage, SearchMessage.Length, "239.255.255.250", 1900);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        while (!linked.IsCancellationRequested)
        {
            PlayerInfo? device = null;
            try
            {
                var packet = await udp.ReceiveAsync(linked.Token);

                var text = Encoding.ASCII.GetString(packet.Buffer);

                var location = GetHeader(text, "LOCATION");
                var usn = GetHeader(text, "USN");

                if (string.IsNullOrWhiteSpace(location))
                    continue;

                device = await GetDeviceInfoAsync(location, usn, linked.Token);

                if (device is null)
                    continue;
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
            catch
            {
                // ignore malformed device
            }

            if (device != null)
            {
                yield return device with { Source = DiscoverySource.Ssdp };
            }
        }
    }

    private async Task<PlayerInfo?> GetDeviceInfoAsync(string location, string? usn, CancellationToken cancellationToken)
    {
        try
        {
            var xml = await _httpClient.GetStringAsync(location, cancellationToken);

            var doc = XDocument.Parse(xml);

            var device = doc.Descendants()
                .FirstOrDefault(static e => string.Equals(e.Name.LocalName, "device", StringComparison.Ordinal));

            if (device is null)
                return null;

            var manufacturer = Get("manufacturer");
            var modelName = Get("modelName");
            var friendlyName = Get("friendlyName");

            // Filter out every non-Oppo UPnP device that answers ssdp:all
            // (routers, TVs, speakers, etc.) and every Oppo model we don't
            // have a known control port for.
            if (!string.Equals(manufacturer, OppoManufacturer, StringComparison.OrdinalIgnoreCase))
                return null;

            if (!ModelsByUpnpName.TryGetValue(modelName, out var model))
                return null;

            var port = GetPort(model);

            return new PlayerInfo(
                DeviceId: usn ?? new Uri(location).Host,
                Host: new Uri(location).Host,
                Port: port,
                DisplayName: friendlyName,
                Source: DiscoverySource.Ssdp,
                Model: model,
                Manufacturer: manufacturer,
                ModelName: modelName,
                FriendlyName: friendlyName,
                Location: location,
                Usn: usn);

            string Get(string name)
            {
                return device.Elements()
                    .FirstOrDefault(e => string.Equals(e.Name.LocalName, name, StringComparison.Ordinal))
                    ?.Value ?? string.Empty;
            }
        }
        catch
        {
            return null;
        }
    }

    private static string? GetHeader(string response, string header)
    {
        var responseSpan = response.AsSpan();
        foreach (var range in responseSpan.Split("\r\n"))
        {
            var line = responseSpan[range];
            if (line.Length <= header.Length + 1 || !line.StartsWith(header, StringComparison.OrdinalIgnoreCase)
                                                 || line[header.Length] != ':')
                continue;

            var readOnlySpan = line[(header.Length + 1)..].Trim();
            return readOnlySpan.IsEmpty ? null : readOnlySpan.ToString();
        }

        return null;
    }

    private static ushort GetPort(OppoModel model) => model switch
    {
        OppoModel.BDP83 => 19999,
        OppoModel.BDP9X or OppoModel.BDP10X => 48360,
        OppoModel.UDP203 or OppoModel.UDP205 => 23,
        _ => throw new InvalidOperationException($"Model {model} is not supported.")
    };
}
