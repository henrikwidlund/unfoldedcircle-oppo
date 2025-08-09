using System.ComponentModel.DataAnnotations;
using Theodicean.SourceGenerators;

namespace UnfoldedCircle.Models.Events;

[JsonConverter(typeof(DriverSetupChangeEventTypeJsonConverter))]
public enum DriverSetupChangeEventType
{
    /// <summary>
    /// setup started
    /// </summary>
    [Display(Name = "START")]
    Start = 1,
    
    /// <summary>
    /// setup in progress. See <see cref="DriverSetupChangeState"/> enum for current setup state.
    /// </summary>
    [Display(Name = "SETUP")]
    Setup,
    
    /// <summary>
    /// setup finished, either with: state: <see cref="DriverSetupChangeState.Ok"/> for successful setup, or state: <see cref="DriverSetupChangeState.Error"/> if setup didn't complete successfully.
    /// </summary>
    [Display(Name = "STOP")]
    Stop
}

[EnumJsonConverter(typeof(DriverSetupChangeEventType), CaseSensitive = false, PropertyName = "event_type")]
public partial class DriverSetupChangeEventTypeJsonConverter;