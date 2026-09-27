using Oppo;

namespace UnfoldedCircle.OppoBluRay.Discovery;

public sealed record PlayerInfo(
    string DeviceId,
    string Host,
    int Port,
    string DisplayName,
    DiscoverySource Source,
    // Best-effort model guess for pre-filling the settings page: certain for
    // Ssdp/Magnetar (the source identifies the exact model), and a text
    // match for the legacy OREMOTE reply.
    OppoModel? Model = null,
    string? Manufacturer = null,
    string? ModelName = null,
    string? FriendlyName = null,
    string? Location = null,
    string? Usn = null);
