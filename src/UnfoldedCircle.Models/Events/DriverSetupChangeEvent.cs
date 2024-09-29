namespace UnfoldedCircle.Models.Events;

public record DriverSetupChangeEvent : CommonEventRequired<DriverSetupChange>;

public record DriverSetupChange
{
    [JsonPropertyName("event_type")]
    public required DriverSetupChangeEventType EventType { get; init; }
    
    [JsonPropertyName("state")]
    public required DriverSetupChangeState State { get; init; }
    
    /// <summary>
    /// More detailed error reason for <see cref="State"/>: <see cref="DriverSetupChangeState.Error"/> condition.
    /// </summary>
    [JsonPropertyName("error")]
    public DriverSetupChangeError? Error { get; init; }
}
