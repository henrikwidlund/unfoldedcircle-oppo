using Oppo;

namespace UnfoldedCircle.OppoBluRay.Configuration;

public record OppoConfigurationItem : UnfoldedCircle.Server.Configuration.UnfoldedCircleConfigurationItem
{
    public required OppoModel Model { get; init; }
    public required bool UseMediaEvents { get; init; }
    public required bool UseStreamingEvents { get; init; }
    public required string? MacAddress { get; init; }

    // Set when Host is a TCP-to-RS-232 bridge rather than the player, which then has no IP of its own:
    // discovery, Wake-on-LAN and the pre-20X HTTP metadata API do not apply. Neither member is required,
    // so configurations written before serial support still load.
    public bool UseSerialBridge { get; init; }
    public ushort? ControlPort { get; init; }
}
