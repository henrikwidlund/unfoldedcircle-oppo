using System.ComponentModel.DataAnnotations;

using UnfoldedCircle.Generators;

namespace UnfoldedCircle.Models.Events;

[JsonConverter(typeof(StateJsonConverter))]
public enum State
{
    [Display(Name = "UNAVAILABLE")]
    Unavailable,
    
    [Display(Name = "UNKNOWN")]
    Unknown,
    
    [Display(Name = "ON")]
    On,
    
    [Display(Name = "OFF")]
    Off,
    
    [Display(Name = "PLAYING")]
    Playing,
    
    [Display(Name = "PAUSED")]
    Paused,
    
    [Display(Name = "STANDBY")]
    Standby,
    
    [Display(Name = "BUFFERING")]
    Buffering
}

[EnumJsonConverter(typeof(State), CaseSensitive = false, PropertyName = "state")]
public partial class StateJsonConverter;