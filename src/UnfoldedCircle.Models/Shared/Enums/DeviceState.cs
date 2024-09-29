using System.ComponentModel.DataAnnotations;
using UnfoldedCircle.Generators;

namespace UnfoldedCircle.Models.Shared;

[JsonConverter(typeof(DeviceStateJsonConverter))]
public enum DeviceState
{
    [Display(Name = "CONNECTED")]
    Connected = 1,
    
    [Display(Name = "CONNECTING")]
    Connecting,
    
    [Display(Name = "DISCONNECTED")]
    Disconnected,
    
    [Display(Name = "ERROR")]
    Error
}

[EnumJsonConverter(typeof(DeviceState), CaseSensitive = false, PropertyName = "state")]
public partial class DeviceStateJsonConverter;