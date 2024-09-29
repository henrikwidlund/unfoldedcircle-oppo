namespace UnfoldedCircle.Models.Sync;

public record SetupDriverMsgData
{
    /// <summary>
    /// If set to <see langword="true"/>: reconfigure an already configured driver.
    /// </summary>
    [JsonPropertyName("reconfigure")]
    public bool? Reconfigure { get; init; }

    /// <summary>
    /// User input result of a <see cref="SettingsPage"/> as key values.
    /// <para>key: id of the field</para>
    /// <para>value: entered user value as string. This is either the entered text or number, selected checkbox state or the selected dropdown item id.</para>
    /// </summary>
    /// <remarks>
    /// Non native string values as numbers or booleans are represented as string values!
    /// </remarks>
    [JsonPropertyName("setup_data")]
    public required Dictionary<string, string> SetupData { get; init; }
}