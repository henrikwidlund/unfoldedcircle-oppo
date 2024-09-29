namespace UnfoldedCircle.Models.Sync;

/// <summary>
/// Get version information about the integration driver.
/// </summary>
public record DriverVersionMsg : CommonRespRequired<DriverVersion>;