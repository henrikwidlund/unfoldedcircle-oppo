using System.ComponentModel.DataAnnotations;
using Theodicean.SourceGenerators;

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