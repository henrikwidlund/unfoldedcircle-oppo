namespace UnfoldedCircle.Models.Events;

public record RemoteOptions
{
    /// <summary>
    /// Optional list of supported commands. If not provided, the integration driver has to document the available commands for the user.
    /// </summary>
    /// <example>["EXIT", "CHANNEL_UP", "CHANNEL_DOWN"]</example>
    [JsonPropertyName("simple_commands")]
    public ISet<string>? SimpleCommands { get; init; }

    [JsonPropertyName("button_mapping")]
    public DeviceButtonMapping[]? ButtonMapping { get; init; }

    [JsonPropertyName("user_interface")]
    public UserInterface? UserInterface { get; init; }
}