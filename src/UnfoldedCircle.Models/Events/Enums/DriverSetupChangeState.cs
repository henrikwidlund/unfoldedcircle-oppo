using System.ComponentModel.DataAnnotations;
using Theodicean.SourceGenerators;

namespace UnfoldedCircle.Models.Events;

[JsonConverter(typeof(DriverSetupChangeStateJsonConverter))]
public enum DriverSetupChangeState
{
    /// <summary>
    /// setup in progress
    /// </summary>
    [Display(Name = "SETUP")]
    Setup = 1,
    
    /// <summary>
    /// setup flow is waiting for user input. See require_user_action for requested input.
    /// </summary>
    [Display(Name = "WAIT_USER_ACTION")]
    WaitUserAction,
    
    /// <summary>
    /// setup finished successfully
    /// </summary>
    [Display(Name = "OK")]
    Ok,
    
    /// <summary>
    /// setup error
    /// </summary>
    [Display(Name = "ERROR")]
    Error
}

[EnumJsonConverter(typeof(DriverSetupChangeState), CaseSensitive = false, PropertyName = "state")]
public partial class DriverSetupChangeStateJsonConverter;