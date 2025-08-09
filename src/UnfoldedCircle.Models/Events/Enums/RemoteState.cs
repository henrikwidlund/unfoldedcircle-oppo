using System.ComponentModel.DataAnnotations;
using Theodicean.SourceGenerators;

namespace UnfoldedCircle.Models.Events;

[JsonConverter(typeof(RemoteStateJsonConverter))]
public enum RemoteState
{
    [Display(Name = "ON")]
    On,

    [Display(Name = "OFF")]
    Off,

    [Display(Name = "UNKNOWN")]
    Unknown
}

[EnumJsonConverter(typeof(RemoteState), CaseSensitive = false, PropertyName = "state")]
public partial class RemoteStateJsonConverter;