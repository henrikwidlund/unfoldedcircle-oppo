using System.ComponentModel.DataAnnotations;

using UnfoldedCircle.Generators;

namespace UnfoldedCircle.Models.Shared;

[JsonConverter(typeof(RepeatModeJsonConverter))]
public enum RepeatMode
{
    [Display(Name = "OFF")]
    Off = 1,
    
    [Display(Name = "ALL")]
    All,
    
    [Display(Name = "ONE")]
    One
}

[EnumJsonConverter(typeof(RepeatMode), CaseSensitive = false, PropertyName = "repeat")]
public partial class RepeatModeJsonConverter;