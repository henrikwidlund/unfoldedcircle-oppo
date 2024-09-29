namespace UnfoldedCircle.Models.Events;

/// <summary>
/// If the user aborts the setup process, the UC Remote sends this event. Further messages from the integration from the setup process will be ignored afterwards.
/// </summary>
public record AbortDriverSetupEvent : CommonEventRequired<IntegrationSetupError>;